﻿using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.QueryBuilding;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Extraction
{
    public class DRSImageExtraction : ImageExtraction
    {
        [DemandsInitialization("The name of the column in the dataset which contains the names of the image files (NOT THE FILENAME IN THE IMAGE ARCHIVE)")]
        public string FilenameColumnName { get; set; }

        public override DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if (!PreProcessingCheck(listener))
                return toProcess;

            // Need to replace the data in the image filename field
            if (!toProcess.Columns.Contains(FilenameColumnName))
                throw new InvalidOperationException("The DataTable does not contain the image filename column '" + FilenameColumnName + "'. The filename is required for the researcher to link between images on disk and entries in the dataset extract.");

            var archiveRepository = new ImageArchiveRepository(PathToImageArchive);
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Using image archive at " + PathToImageArchive));

            var imageExtractionPath = Request.Directory.GetDirectoryForDataset(Request.DatasetBundle.DataSet).CreateSubdirectory("Images");
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Images will be saved to " + imageExtractionPath.FullName));

            var columnsToExtract = Request.QueryBuilder.SelectColumns.ToList();
            if (columnsToExtract == null)
                throw new InvalidOperationException("The request does not contain a list of extractable columns.");

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, columnsToExtract.Count + " extractable columns found"));

            var extractionIdentifier = columnsToExtract.SingleOrDefault(c => c.IColumn.IsExtractionIdentifier);
            if (extractionIdentifier == null)
                throw new InvalidOperationException("The request does not contain a column marked as IsExtractionIdentifier.");

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Extraction identifier column = " + extractionIdentifier.IColumn.GetRuntimeName()));

            var replacer = new DRSFilenameReplacer(extractionIdentifier.IColumn, FilenameColumnName);
            
            var progress = 0;
            var extractionMap = new Dictionary<string, Dictionary<string, string>>();

            var sw = new Stopwatch();
            sw.Start();

            // Process data table, replacing FilenameColumnName, and build the extraction map
            foreach (DataRow row in toProcess.Rows)
            {
                progress++;

                if (string.IsNullOrWhiteSpace(row[ImageUriColumnName].ToString()))
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Row " + progress + " does not have a corresponding image, [" + ImageUriColumnName + "] is empty."));
                    continue;
                }

                listener.OnProgress(this, new ProgressEventArgs("Replacing filenames...", new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));
                var newFilename = replacer.GetCorrectFilename(row, listener);

                // Replace the filename column in the dataset, so it no longer contains CHI
                row[FilenameColumnName] = newFilename;

                // Build the extraction map
                var uri = row[ImageUriColumnName].ToString();
                var parts = uri.Split('!');
                var archiveName = parts[0];
                var archivePath = Path.Combine(PathToImageArchive, archiveName);
                var entry = parts[1];

                if (!extractionMap.ContainsKey(archivePath))
                {
                    extractionMap.Add(archivePath, new Dictionary<string, string>());
                }

                extractionMap[archivePath].Add(entry, Path.Combine(imageExtractionPath.FullName, newFilename));
            }

            // Now extract the images from the archives
            progress = 0;
            sw.Restart();
            foreach (var entry in extractionMap)
            {
                listener.OnProgress(this, new ProgressEventArgs("Extracting images from archives...", new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));

                var archiveType = Path.GetExtension(entry.Key);
                
                switch (archiveType)
                {
                    case ".tar":
                        archiveRepository.ExtractImageSetFromTar(entry.Key, entry.Value);
                        break;
                    case ".zip":
                        archiveRepository.ExtractImageSetFromZip(entry.Key, entry.Value);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported image archive type: " + archiveType + " (only tar and zip files are supported)");
                }

                progress += entry.Value.Count;
            }

            // Drop the ImageUriColumnName column
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Removing the '" + ImageUriColumnName + "' from the dataset."));
            toProcess.Columns.Remove(ImageUriColumnName);

            return toProcess;
        }

        public override void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
        }

        public override void Abort(IDataLoadEventListener listener)
        {
        }

        public override void Check(ICheckNotifier notifier)
        {
            List<IColumn> columns = Request.ColumnsToExtract;

            if (!columns.Any(c => c.GetRuntimeName() == ImageUriColumnName))
                notifier.OnCheckPerformed(new CheckEventArgs("Expected column " + ImageUriColumnName + " (points to the image in the archive) but it has not been configured for extraction.", CheckResult.Fail));
            else
                notifier.OnCheckPerformed(new CheckEventArgs("Found expected column " + ImageUriColumnName, CheckResult.Success));

            if (!columns.Any(c => c.GetRuntimeName() == FilenameColumnName))
                notifier.OnCheckPerformed(new CheckEventArgs("Expected column " + FilenameColumnName + " (contains the original filename of the DRS image) but it has not been configured for extraction.", CheckResult.Fail));
            else
                notifier.OnCheckPerformed(new CheckEventArgs("Found expected column " + FilenameColumnName, CheckResult.Success));

            if (!Directory.Exists(PathToImageArchive))
            {
                notifier.OnCheckPerformed(new CheckEventArgs("The image archive was not found (configured in PathToImageArchive): " + PathToImageArchive, CheckResult.Fail));
                return;
            }

            notifier.OnCheckPerformed(new CheckEventArgs("Found image archive at " + PathToImageArchive, CheckResult.Success));

            if (!Directory.EnumerateFileSystemEntries(PathToImageArchive, "*", SearchOption.AllDirectories).Any())
                notifier.OnCheckPerformed(new CheckEventArgs("The image archive was found (configured in PathToImageArchive) but is empty: " + PathToImageArchive, CheckResult.Fail));
            else
                notifier.OnCheckPerformed(new CheckEventArgs("Image archive is not empty (that's the best check we can do at the moment)", CheckResult.Success));
        }
    }
}