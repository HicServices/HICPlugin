using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin;
using SCIStorePlugin.Cache.Pipeline;
using SCIStorePlugin.Data;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

public class SCIStoreCacheDestinationTests : DatabaseTests
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
    public void TearDown()
    {
        _directoryHelper.TearDown();
    }

    /// <summary>
    /// Simple test to determine whether the cache destination component correctly creates an archived zip file given dummy data in a cache chunk
    /// </summary>
    [Test]
    public void ProcessPipelineDataTest()
    {
        var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("ProcessPipelineDataTest");
        var component = new SCIStoreCacheDestination
        {
            HealthBoard = HealthBoard.T,
            Discipline =  Discipline.Biochemistry,
            CacheDirectory = rootDirectory,
            SilentRunning = true,
        };
            
        // this would be provided by a previous component in the caching data flow pipeline
        var report = new CombinedReportData
        {
            HbExtract = HealthBoard.T.ToString(),
            SciStoreRecord = new SciStoreRecord
            {
                LabNumber = "123456",
                TestReportID = "999"
            },
            InvestigationReport = new InvestigationReport
            {
                ReportData = new InvestigationReportMessageType
                {
                        
                }
            }
        };
        var fetchDate = new DateTime(2015, 1, 1);
     
            
        var deleteMe = LoadDirectory.CreateDirectoryStructure(new DirectoryInfo(TestContext.CurrentContext.WorkDirectory),"DeleteMe", true);
        try
        {
            var fetchRequest = new Moq.Mock<ICacheFetchRequest>().Object;
                
            var cacheChunk = new SCIStoreCacheChunk(new[] { report }, fetchDate, fetchRequest)
            {
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Biochemistry
            };


            component.PreInitialize(deleteMe,ThrowImmediatelyDataLoadEventListener.Quiet);
            component.ProcessPipelineData((ICacheChunk)cacheChunk, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

            var downloadDir = Path.Combine(rootDirectory.FullName, "T", "Biochemistry");
            var expectedArchiveFilepath = Path.Combine(downloadDir, "2015-01-01.zip");
            Assert.IsTrue(File.Exists(expectedArchiveFilepath));

            // make sure that the archiver has cleaned up after itself
            Assert.IsEmpty(Directory.EnumerateFiles(downloadDir, "*.xml"));
        }
        finally
        {
            Directory.Delete(deleteMe.RootPath.FullName,true);
                
        }
    }
        
}