using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Cache
{
    [Export(typeof(ICacheRebuilder))]
    public class SCIStoreCacheRebuilder : ICacheRebuilder
    {
        private const string FilenameDateFormat = "yyyy-MM-dd";

        [ImportingConstructor]
        public SCIStoreCacheRebuilder()
        {
        }

        public async Task RebuildCacheFromArchiveFiles(string[] filenameList, string destinationPath, IDataLoadEventListener listener, CancellationToken token)
        {
            Contract.Requires<ArgumentNullException>(filenameList != null);
            Contract.Requires<DirectoryNotFoundException>(Directory.Exists(destinationPath));

            var message = filenameList.Count() == 1
                ? "Rebuilding archive from " + filenameList[0]
                : "Rebuilding archive from " + filenameList.Count() + " files: " + filenameList[0] + ", etc.";

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, message));
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Saving files to " + destinationPath));

            var task = Task.Run(() =>
            {
                foreach (var filename in filenameList)
                {
                    token.ThrowIfCancellationRequested();
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Processing " + filename));
                    RefillFromArchive(filename, destinationPath, listener, token);
                }
            }, token);

            await task;
        }

        private void RefillFromArchive(string archiveFilename, string destinationPath, IDataLoadEventListener listener, CancellationToken token)
        {
            Contract.Requires<DirectoryNotFoundException>(File.Exists(archiveFilename));
            
            var archiveFile = new FileInfo(archiveFilename);

            var historyDir = new RootHistoryDirectory(new DirectoryInfo(destinationPath));

            var dateDirsToArchive = new List<string>();

            // read files directly from archive
            // the SCIStore labs data is cached by date, so build up a map of which files should be archived
            var serialise = new XmlSerializer(typeof(CombinedReportData));
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Opening archived job file: " + archiveFile.FullName));

            // Create a temp working dir and extract the enclosing zip
            var tmpDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            tmpDir.Create();
            
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Extracting outer zip file to : " + tmpDir.FullName));
            ZipFile.ExtractToDirectory(archiveFile.FullName, tmpDir.FullName);

            foreach (var innerArchiveFile in Directory.EnumerateFiles(tmpDir.FullName))
            {
                using (var internalArchive = ZipFile.OpenRead(innerArchiveFile))
                {
                    var sw = new Stopwatch();
                    var numProcessed = 0;
                    var numEntries = internalArchive.Entries.Count;
                    sw.Start();
                    foreach (var reportEntry in internalArchive.Entries)
                    {
                        token.ThrowIfCancellationRequested();

                        using (var internalStream = reportEntry.Open())
                        {
                            var report = serialise.Deserialize(internalStream) as CombinedReportData;
                            var destFileInfo = GetFileInfoForCache(report, historyDir, dateDirsToArchive);

                            reportEntry.ExtractToFile(destFileInfo.FullName);

                            ++numProcessed;
                            listener.OnProgress(this,
                                new ProgressEventArgs("Processing " + numEntries + " entries in " + innerArchiveFile,
                                    new ProgressMeasurement(numProcessed, ProgressType.Records), sw.Elapsed));
                        }
                    }
                }
            }

            tmpDir.Delete(true);

            // Now the files are all within date directories under the correct subdirectory in the cache
            ArchiveDateDirectories(dateDirsToArchive, listener);
        }

        private void ArchiveDateDirectories(IEnumerable<string> dateDirsToArchive, IDataLoadEventListener listener)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Now creating the cache archives."));

            foreach (var dateDirName in dateDirsToArchive)
            {
                var dateDir = new DirectoryInfo(dateDirName);
                var zipArchiveFile = new FileInfo(dateDir.FullName + ".zip");

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Creating " + zipArchiveFile.FullName));

                if (zipArchiveFile.Exists)
                {
                    //throw new Exception(
                    //    "We are trying to refill the cache from an archived job, but a cache file already exists for this date (" +
                    //    dateDirName +
                    //    "). Something is probably invalid here, not sure if we want to be able to insert into existing cached data. Also, this should have been picked up by an earlier check (CheckThatArchivedReportsAreNotAlreadyInCache)");

                    // Check to see if the entry exists in the zip file
                    using (var zipFile = ZipFile.Open(zipArchiveFile.FullName, ZipArchiveMode.Update))
                    {
                        foreach (var file in dateDir.EnumerateFiles())
                        {
                            var entry = zipFile.GetEntry(file.Name);
                            if (entry == null)
                                zipFile.CreateEntryFromFile(file.FullName, file.Name);
                            else
                                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, file.Name + " is already present in " + zipArchiveFile.FullName));
                        }
                    }
                }
                else
                    ZipFile.CreateFromDirectory(dateDir.FullName, zipArchiveFile.FullName);

                dateDir.Delete(true);
            }
        }

        private FileInfo GetFileInfoForCache(CombinedReportData report, RootHistoryDirectory historyDir, List<string> dateDirsToArchive)
        {
            var cacheIdentifiers = GetCacheDirectoryIdentifiers(report);
            var hb = cacheIdentifiers.HealthBoard;
            var discipline = cacheIdentifiers.Discipline;
            var reportDate = cacheIdentifiers.ReportDate;

            // Create the relevant dirs
            historyDir.CreateIfNotExists(hb, discipline);
            var dateDirName = Path.Combine(historyDir[hb][discipline].FullName, reportDate.ToString(FilenameDateFormat));
            var dateDir = new DirectoryInfo(dateDirName);
            if (!dateDir.Exists)
                dateDir.Create();

            if (!dateDirsToArchive.Contains(dateDirName))
                dateDirsToArchive.Add(dateDirName);

            // move the report file into the correct directory in the cache
            var filename = SCIStoreLoadCachePathResolver.GetFilename(report);
            var destFile = new FileInfo(Path.Combine(dateDir.FullName, filename));
            if (destFile.Exists)
                throw new Exception("The file '" + filename + "' already exists in destination directory '" + dateDir.FullName + "'");

            return destFile;
        }

        private CacheDirectoryIdentifiers GetCacheDirectoryIdentifiers(CombinedReportData report)
        {
            HealthBoard hb;
            if (!Enum.TryParse(report.HbExtract, out hb))
                throw new Exception("Could not parse '" + report.HbExtract + "' as a valid HealthBoard from report '" + SCIStoreLoadCachePathResolver.GetFilename(report) + "'");

            Discipline discipline;
            if (!Enum.TryParse(report.InvestigationReport.ReportData.Discipline, out discipline))
                throw new Exception("Could not parse '" + report.SciStoreRecord.Dept + "' as a valid Discipline from report '" + SCIStoreLoadCachePathResolver.GetFilename(report) + "'");

            return new CacheDirectoryIdentifiers
            {
                HealthBoard = hb,
                Discipline = discipline,
                ReportDate = report.InvestigationReport.ReportData.ReportDate
            };

        }
    }
}