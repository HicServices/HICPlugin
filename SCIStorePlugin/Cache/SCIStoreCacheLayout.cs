using System;
using System.IO;
using System.Linq;
using Rdmp.Core.Caching.Layouts;
using Rdmp.Core.Caching.Pipeline.Destinations;
using Rdmp.Core.Curation.Data.DataLoad;
using ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Cache;

public class SCIStoreCacheLayout : CacheLayout
{

    private RootHistoryDirectory _rootHistoryDirectory;
      
    public SCIStoreCacheLayout(DirectoryInfo cacheDirectory, SCIStoreLoadCachePathResolver resolver): base(cacheDirectory, "yyyy-MM-dd", CacheArchiveType.Zip, CacheFileGranularity.Day, resolver)
    {
        _rootHistoryDirectory = new RootHistoryDirectory(cacheDirectory);
    }

    public void Cleanup()
    {
        _rootHistoryDirectory.CleanupLingeringXMLFiles();
    }

    public void CreateArchive(DateTime archiveDate)
    {
        var downloadDirectory = GetLoadCacheDirectory(new ThrowImmediatelyDataLoadEventListener());
        var dataFiles = downloadDirectory.EnumerateFiles("*.xml").ToArray();
        ArchiveFiles(dataFiles, archiveDate,new ThrowImmediatelyDataLoadEventListener());
        Cleanup();
    }

    public void ValidateLayout()
    {
        // todo: ask rootHistoryDirectory to validate the structure (actually, rootHistoryDirectory functionality is closely-related to CacheLayout)
        _rootHistoryDirectory.Validate();
    }
}