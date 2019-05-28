using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using Rdmp.Core.Curation;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Cache;
using SCIStorePlugin.Data;

namespace SCIStorePlugin
{
    public delegate void CacheRefillProgressHandler(NotifyEventArgs notifyEventArgs);
    public delegate void CacheRefillCompleteHandler();

    public class CacheRefiller
    {
        private readonly LoadDirectory _hicProjectDirectory;
        private readonly string _filenameDateFormat;

        public event CacheRefillProgressHandler CacheRefillProgress;
        public event CacheRefillCompleteHandler CacheRefillComplete;

        protected virtual void OnCacheRefillComplete()
        {
            var handler = CacheRefillComplete;
            if (handler != null) handler();
        }

        protected virtual void OnCacheRefillProgress(NotifyEventArgs notifyEventArgs)
        {
            var handler = CacheRefillProgress;
            if (handler != null) handler(notifyEventArgs);
        }

        public CacheRefiller(LoadDirectory hicProjectDirectory, string filenameDateFormat)
        {
            _hicProjectDirectory = hicProjectDirectory;
            _filenameDateFormat = filenameDateFormat;
        }

        public void RefillFromArchive(string archiveFilename)
        {
            var archiveFile = new FileInfo(Path.Combine(_hicProjectDirectory.ForArchiving.FullName, archiveFilename));
            if (!archiveFile.Exists)
                throw new FileNotFoundException(
                    "The archive file " + archiveFilename + " was not found in the archive directory '" +
                    _hicProjectDirectory.ForArchiving.FullName + "'", archiveFilename);

            OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Archived reports are not present in the cache so can continue..."));

            var historyDir = new RootHistoryDirectory(_hicProjectDirectory.Cache);

            var dateDirsToArchive = new List<string>();

            // read files directly from archive
            // the SCIStore labs data is cached by date, so build up a map of which files should be archived
            var serialise = new XmlSerializer(typeof(CombinedReportData));
            OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Opening archived job file: " + archiveFile.FullName));
            using (var zipFile = ZipFile.OpenRead(archiveFile.FullName))
            {
                OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, zipFile.Entries.Count + " reports"));

                var numProcessed = 0;
                foreach (var entry in zipFile.Entries)
                {
                    using (var stream = entry.Open())
                    {
                        var report = serialise.Deserialize(stream) as CombinedReportData;
                        var destFileInfo = GetFileInfoForCache(report, historyDir, dateDirsToArchive);

                        entry.ExtractToFile(destFileInfo.FullName);
                        ++numProcessed;

                        if (numProcessed%1000 == 0)
                            OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Processed " + numProcessed + " out of " + zipFile.Entries.Count));
                    }
                }
            }

            // Now the files are all within date directories under the correct subdirectory in the cache
            ArchiveDateDirectories(dateDirsToArchive);

            OnCacheRefillComplete();
        }

        private void ArchiveDateDirectories(IEnumerable<string> dateDirsToArchive)
        {
            OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Now creating the cache archives."));

            foreach (var dateDirName in dateDirsToArchive)
            {
                var dateDir = new DirectoryInfo(dateDirName);
                var zipArchiveFile = new FileInfo(dateDir.FullName + ".zip");

                OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Creating " + zipArchiveFile.FullName));
                
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
                                OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Warning, file.Name + " is already present in " + zipArchiveFile.FullName));
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
            var dateDirName = Path.Combine(historyDir[hb][discipline].FullName, reportDate.ToString(_filenameDateFormat));
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

        private DirectoryInfo CreateScratchDir()
        {
            var scratchDirName = "CacheRefill_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var scratchDir = _hicProjectDirectory.RootPath.CreateSubdirectory(scratchDirName);
            return scratchDir;
        }

        public void CheckThatArchivedReportsAreNotAlreadyInCache(FileInfo archiveFile)
        {
            var cacheIdentifiersChecked = new List<CacheDirectoryIdentifiers>();
            var historyDir = new RootHistoryDirectory(_hicProjectDirectory.Cache);

            var serialise = new XmlSerializer(typeof (CombinedReportData));
            using (var zipFile = ZipFile.OpenRead(archiveFile.FullName))
            {
                OnCacheRefillProgress(new NotifyEventArgs(ProgressEventType.Information, "Checking the reports from " + archiveFile.FullName + " are not already present in the cache"));
                foreach (var entry in zipFile.Entries)
                {
                    using (var stream = entry.Open())
                    {
                        var report = serialise.Deserialize(stream) as CombinedReportData;
                        var cacheIdentifiers = GetCacheDirectoryIdentifiers(report);
                        if (!cacheIdentifiersChecked.Contains(cacheIdentifiers))
                        {
                            // check that there is not a cache file which would contain this report
                            if (!historyDir.ContainsKey(cacheIdentifiers.HealthBoard))
                                continue; // there is no directory in the cache for this health board

                            if (!historyDir[cacheIdentifiers.HealthBoard].ContainsKey(cacheIdentifiers.Discipline))
                                continue; // there is no directory in the cache for this health board + discipline combo

                            var cacheFile = Path.Combine(historyDir[cacheIdentifiers.HealthBoard][cacheIdentifiers.Discipline].FullName, cacheIdentifiers.ReportDate.ToString(_filenameDateFormat)) + ".zip";
                            if (File.Exists(cacheFile))
                                throw new Exception("This archived job contains a report which is from a day already present in the cache: " + cacheIdentifiers + ". This job will need to be investigated and refilled manually.");

                            cacheIdentifiersChecked.Add(cacheIdentifiers);
                        }
                    }
                }
            }
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

    internal class CacheDirectoryIdentifiers
    {
        public HealthBoard HealthBoard { get; set; }
        public Discipline Discipline { get; set; }
        public DateTime ReportDate { get; set; }

        public override string ToString()
        {
             return HealthBoard + " - " + Discipline + " - " + ReportDate;
        }
    }
}
