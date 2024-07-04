using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Caching.Pipeline;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline.Requirements;
using SCIStorePlugin.Cache.Pipeline;
using System.IO;
using Tests.Common;

namespace SCIStorePluginTests.Unit;

public class ContextTests : DatabaseTests
{
    [Test]
    public void Context_LegalSource()
    {
        var testDirHelper = new TestDirectoryHelper(GetType());
        testDirHelper.SetUp();

        var projDir = LoadDirectory.CreateDirectoryStructure(testDirHelper.Directory, "Test", true);
        var lmd = new LoadMetadata(CatalogueRepository)
        {
            LocationOfForLoadingDirectory = Path.Join(projDir.RootPath.FullName, "Data", "ForLoading"),
            LocationOfForArchivingDirectory = Path.Join(projDir.RootPath.FullName, "Data", "ForArchiving"),
            LocationOfCacheDirectory = Path.Join(projDir.RootPath.FullName, "Cahce"),
            LocationOfExecutablesDirectory = Path.Join(projDir.RootPath.FullName, "Executables"),
        };
        lmd.SaveToDatabase();

        var cp = new MockCacheProgress(lmd);

        var provider = new MockCacheFetchRequestProvider();

        var useCase = new CachingPipelineUseCase(cp, true, provider);
        var cacheContext = (DataFlowPipelineContext<ICacheChunk>)useCase.GetContext();

        //we shouldn't be able to have data export sources in this context
        Assert.That(cacheContext.IsAllowable(typeof(SCIStoreWebServiceSource)), Is.True);
    }
}