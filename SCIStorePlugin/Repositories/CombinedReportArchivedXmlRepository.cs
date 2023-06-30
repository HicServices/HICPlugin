using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class CombinedReportArchivedXmlRepository : IRepository<CombinedReportData>
{
    private readonly DirectoryInfo _sourceDirectory;
        
    public CombinedReportArchivedXmlRepository(DirectoryInfo sourceDirectory)
    {
        _sourceDirectory = sourceDirectory;
    }

    public IEnumerable<CombinedReportData> ReadAll()
    {
        var reports = new List<CombinedReportData>();

        if (_sourceDirectory.EnumerateFiles().Any(info => !info.Extension.Equals(".zip")))
            throw new Exception(
                $"There are some non-zip files in '{_sourceDirectory.FullName}'. This directory should *only* contain zip archives.");

        var archives = _sourceDirectory.EnumerateFiles("*.zip", SearchOption.AllDirectories);

        var deserializer = new CombinedReportXmlDeserializer();
        foreach (var archive in archives)
        {
            try
            {
                using var zipArchive = ZipFile.Open(archive.FullName, ZipArchiveMode.Read);
                reports.AddRange(zipArchive.Entries.Select(entry => deserializer.DeserializeFromZipEntry(entry, Path.Combine(archive.FullName, entry.Name))));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error while reading archive: {archive.FullName}", e);
            }
        }

        return reports;
    }

    public string FindArchiveContainingReport(CombinedReportData report)
    {
        var archives = _sourceDirectory.EnumerateFiles("*.zip", SearchOption.AllDirectories);

        foreach (var archive in archives)
        {
            using var zipArchive = ZipFile.Open(archive.FullName, ZipArchiveMode.Read);
            if (zipArchive.Entries.Select(entry => entry.Name.Split(new [] {'-', '.'})).Any(nameParts => nameParts[1].Equals(report.SciStoreRecord.LabNumber) &&
                    nameParts[2].Equals(report.SciStoreRecord.TestReportID)))
            {
                return archive.FullName;
            }
        }
        return null;
    }

    public IEnumerable<CombinedReportData> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<CombinedReportData>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
        throw new NotImplementedException();
    }
}