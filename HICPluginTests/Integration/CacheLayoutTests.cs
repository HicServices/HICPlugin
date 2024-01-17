using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Cache;
using SCIStorePlugin.Data;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

[Category("Integration")]
class CacheLayoutTests : DatabaseTests
{
    private TestDirectoryHelper _directoryHelper;

    [OneTimeSetUp]
    protected override void SetUp()
    {
        base.SetUp();

        _directoryHelper = new TestDirectoryHelper(GetType());
        _directoryHelper.SetUp();
    }

    [OneTimeTearDown]
    public new void TearDown()
    {
        _directoryHelper.TearDown();
        base.TearDown();
    }

    // Needs Database for MEF
    [Test, Category("Database")]
    public void TestFactoryConstruction()
    {
        var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("TestFactoryConstruction");
            
        var hbDir = rootDirectory.CreateSubdirectory("T");
        hbDir.CreateSubdirectory("Biochemistry");
            
            
        var cacheLayout = new SCIStoreCacheLayout(rootDirectory,new SCIStoreLoadCachePathResolver(HealthBoard.T, Discipline.Biochemistry));
        var downloadDirectory = cacheLayout.GetLoadCacheDirectory(ThrowImmediatelyDataLoadEventListener.Quiet);

        var expectedDownloadPath = Path.Combine(rootDirectory.FullName, "T", "Biochemistry");
        Assert.Equals(expectedDownloadPath, downloadDirectory.FullName);
    }

    [Test]
    public void TestCachingCombinedReports()
    {
        var cacheDirectory = _directoryHelper.Directory.CreateSubdirectory("TestCachingCombinedReports");
        var cacheLayout = new SCIStoreCacheLayout(cacheDirectory,new SCIStoreLoadCachePathResolver(HealthBoard.T,Discipline.Biochemistry));

        // the chunk of cache data to be saved
        var fetchDate = new DateTime(2005, 1, 1);

        cacheLayout.CreateIfNotExists(ThrowImmediatelyDataLoadEventListener.Quiet);

        var expectedDownloadDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "T", "Biochemistry"));
        Assert.That(expectedDownloadDirectory.Exists, Is.True);//"The cache has not created directory into which the individual report files will be copied");

        // stick some dummy files in the directory and check if archiving works
        File.WriteAllText(Path.Combine(expectedDownloadDirectory.FullName, "12345.xml"), "Test");

        cacheLayout.CreateArchive(fetchDate);
        var archiveFile = cacheLayout.GetArchiveFileInfoForDate(fetchDate, ThrowImmediatelyDataLoadEventListener.Quiet);
        Assert.That(archiveFile.Exists,Is.True);

        const string expectedArchiveFilename = "2005-01-01.zip";
        Assert.Equals(expectedArchiveFilename, archiveFile.Name);
    }
}