using Moq;
using NUnit.Framework;
using Rdmp.Core.Caching.Pipeline;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Caching.Requests.FetchRequestProvider;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataFlowPipeline.Requirements;
using SCIStorePlugin.Cache.Pipeline;
using Tests.Common;

namespace SCIStorePluginTests.Unit;

public class ContextTests : DatabaseTests
{
    [Test]
    public void Context_LegalSource()
    {
        var cp = new Mock<ICacheProgress>();
        cp.Setup(p => p.Pipeline).Returns(new Mock<IPipeline>().Object);

        var lmd = new LoadMetadata(CatalogueRepository);

        var testDirHelper = new TestDirectoryHelper(GetType());
        testDirHelper.SetUp();

        var projDir = LoadDirectory.CreateDirectoryStructure(testDirHelper.Directory, "Test", true);
        lmd.LocationOfFlatFiles = projDir.RootPath.FullName;
        lmd.SaveToDatabase();
            
        var lp = new Mock<ILoadProgress>();
        lp.Setup(m => m.LoadMetadata).Returns(lmd);

        cp.Setup(m => m.LoadProgress).Returns(lp.Object);
            
        var provider = new Mock<ICacheFetchRequestProvider>().Object;

        var useCase = new CachingPipelineUseCase(cp.Object, true, provider);
        var cacheContext = (DataFlowPipelineContext<ICacheChunk>)useCase.GetContext();

        //we shouldn't be able to have data export sources in this context
        Assert.IsTrue(cacheContext.IsAllowable(typeof(SCIStoreWebServiceSource)));
    }
}