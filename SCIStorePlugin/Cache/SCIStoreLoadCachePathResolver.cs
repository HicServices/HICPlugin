using System;
using System.IO;
using Rdmp.Core.Caching.Layouts;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Cache;

// Implements specific logic for creating paths/archives
// Dependent on the specific type of chunk that is being saved. (I think this makes sense as the cache chunk should contain all the information required to determine the correct filepaths in the archive)
public class SCIStoreLoadCachePathResolver : ILoadCachePathResolver
{
    private readonly HealthBoard _healthBoard;
    private readonly Discipline _discipline;

    public SCIStoreLoadCachePathResolver(HealthBoard healthboard, Discipline discipline)
    {
        _healthBoard = healthboard;
        _discipline = discipline;
    }

    public static string GetFilename(CombinedReportData report)
    {
        if (report.SciStoreRecord == null)
            throw new Exception("This CombinedReportData does not have a SciStoreRecord object, which is required for recovering LabNumber and TestReportID");

        var record = report.SciStoreRecord;
        if (string.IsNullOrWhiteSpace(record.LabNumber))
            throw new Exception("This report has no LabNumber (check construction)");

        if (string.IsNullOrWhiteSpace(record.TestReportID))
            throw new Exception("This report has no TestReportID (check construction)");

        return $"report-{report.SciStoreRecord.LabNumber}-{report.SciStoreRecord.TestReportID}.xml";
    }

    public DirectoryInfo GetLoadCacheDirectory(DirectoryInfo rootDirectory)
    {
        var downloadDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, GetRelativePath()));
        return downloadDirectory.Exists ? downloadDirectory : Directory.CreateDirectory(downloadDirectory.FullName);
    }

    private string GetRelativePath()
    {
        return Path.Combine(_healthBoard.ToString(), _discipline.ToString());
    }
}