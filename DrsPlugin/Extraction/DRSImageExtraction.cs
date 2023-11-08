using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Extraction;

public sealed class DRSImageExtraction : ImageExtraction
{
    [DemandsInitialization("The name of the column in the dataset which contains the names of the image files (NOT THE FILENAME IN THE IMAGE ARCHIVE)")]
    public string FilenameColumnName { get; set; }

    [DemandsInitialization("A comma separated list of columns to use to de-identify the image files. The order if this list will set the order of replacement within the extraction. RDMP will prepend the ReleaseID. Historical Releases will typically want to use 'Examination_Date,Image_Num'")]
    public string FileNameReplacementColumns { get; set; }

    [DemandsInitialization("This will add a number to the end of each file name to prevent duplicate file names. Remove this if your data source has it's own unique identifiers")]
    public bool AppendIndexCountToFileName { get; set; } = true;

    public override DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (!PreProcessingCheck(listener))
            return toProcess;

        // Need to replace the data in the image filename field
        if (!toProcess.Columns.Contains(FilenameColumnName))
            throw new InvalidOperationException(
                $"The DataTable does not contain the image filename column '{FilenameColumnName}'. The filename is required for the researcher to link between images on disk and entries in the dataset extract.");

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Using image archive at {PathToImageArchive}"));

        var imageExtractionPath = Request.Directory.GetDirectoryForDataset(Request.DatasetBundle.DataSet).CreateSubdirectory("Images");
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Images will be saved to {imageExtractionPath.FullName}"));

        var columnsToExtract = Request.QueryBuilder.SelectColumns?.ToList() ?? throw new InvalidOperationException("The request does not contain a list of extractable columns.");
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"{columnsToExtract.Count} extractable columns found"));

        var extractionIdentifier = columnsToExtract.SingleOrDefault(static c => c.IColumn.IsExtractionIdentifier) ?? throw new InvalidOperationException("The request does not contain a column marked as IsExtractionIdentifier.");
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Extraction identifier column = {extractionIdentifier.IColumn.GetRuntimeName()}"));

        var replacer = new DRSFilenameReplacer(extractionIdentifier.IColumn, FilenameColumnName);

        var progress = 0;
        var extractionMap = new Dictionary<string, Dictionary<string, string>>();

        var sw = Stopwatch.StartNew();

        // Process data table, replacing FilenameColumnName, and build the extraction map
        foreach (DataRow row in toProcess.Rows)
        {
            progress++;

            listener.OnProgress(this, new ProgressEventArgs("Replacing filenames...", new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));
            if (string.IsNullOrWhiteSpace(row[ImageUriColumnName].ToString()))
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                    $"Row {progress} does not have a corresponding image, [{ImageUriColumnName}] is empty. Will strip out the file name."));
                row[FilenameColumnName] = "";
                continue;
            }
            var fileNameReplacementColumns = FileNameReplacementColumns is not null ? FileNameReplacementColumns.Split(',') : Array.Empty<string>();
            if (fileNameReplacementColumns.Length == 0) listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "No filename replacement columns are specified. This is a good way to leak PII"));

            var newFilename = replacer.GetCorrectFilename(row, fileNameReplacementColumns, AppendIndexCountToFileName ? progress : null); //progress used to prevent duplicate file names

            // Replace the filename column in the dataset, so it no longer contains CHI
            row[FilenameColumnName] = newFilename;
            newFilename = Path.Combine(imageExtractionPath.FullName, newFilename);

            // Skip existing - JS 2023-08-15
            if (File.Exists(newFilename))
                continue;

            // Build the extraction map
            var sourceFileName = row[ImageUriColumnName].ToString();

            // Fast path for pre-extracted files - JS 2023-07-10
            if (!sourceFileName.Contains('!'))
            {
                try
                {
                    File.Copy(Path.Combine(PathToImageArchive, sourceFileName), newFilename);
                }
                catch (Exception)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                               $"Failed to copy file ({sourceFileName}) to its new redacted filename."));
                }

                continue;
            }

            var parts = sourceFileName.Split('!');
            var archiveName = parts[0];
            var archivePath = Path.Combine(PathToImageArchive, archiveName);
            var entry = parts[1];

            if (!extractionMap.ContainsKey(archivePath))
            {
                extractionMap.Add(archivePath, new Dictionary<string, string>());
            }

            extractionMap[archivePath].Add(entry, newFilename);
        }

        // Now extract the images from the archives
        progress = 0;
        sw.Restart();
        foreach (var entry in extractionMap)
        {
            listener.OnProgress(this, new ProgressEventArgs("Extracting images from archives...", new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));
            ImageArchiveRepository.ExtractImageSetFromArchive(entry.Key, entry.Value);
            progress += entry.Value.Count;
        }

        // Drop the ImageUriColumnName column
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Removing the '{ImageUriColumnName}' from the dataset."));
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
        if (Request is null)
        {
            return;
        }
        var columns = Request.ColumnsToExtract;

        if (columns.All(c => c.GetRuntimeName() != ImageUriColumnName))
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Expected column {ImageUriColumnName} (points to the image in the archive) but it has not been configured for extraction.", CheckResult.Fail));
        else
            notifier.OnCheckPerformed(new CheckEventArgs($"Found expected column {ImageUriColumnName}", CheckResult.Success));

        if (columns.All(c => c.GetRuntimeName() != FilenameColumnName))
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Expected column {FilenameColumnName} (contains the original filename of the DRS image) but it has not been configured for extraction.", CheckResult.Warning));
        else
            notifier.OnCheckPerformed(new CheckEventArgs($"Found expected column {FilenameColumnName}", CheckResult.Success));

        if (!Directory.Exists(PathToImageArchive))
        {
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"The image archive was not found (configured in PathToImageArchive): {PathToImageArchive}", CheckResult.Fail));
            return;
        }

        notifier.OnCheckPerformed(new CheckEventArgs($"Found image archive at {PathToImageArchive}", CheckResult.Success));

        if (!Directory.EnumerateFileSystemEntries(PathToImageArchive, "*", SearchOption.AllDirectories).Any())
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"The image archive was found (configured in PathToImageArchive) but is empty: {PathToImageArchive}", CheckResult.Fail));
        else
            notifier.OnCheckPerformed(new CheckEventArgs("Image archive is not empty (that's the best check we can do at the moment)", CheckResult.Success));

    }

}