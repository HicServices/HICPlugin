using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Caching.Pipeline;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline.Requirements;
using SCIStorePlugin.Cache.Pipeline;
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
            LocationOfFlatFiles = projDir.RootPath.FullName
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