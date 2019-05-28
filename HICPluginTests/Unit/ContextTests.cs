using NUnit.Framework;
using Tests.Common;

namespace SCIStorePluginTests.Unit
{
    public class ContextTests : DatabaseTests
    {
        [Test]
        public void Context_LegalSource()
        {
            var cp = MockRepository.GenerateMock<ICacheProgress>();
            cp.Expect(p => p.Pipeline).Return(MockRepository.GenerateMock<IPipeline>());

            var lmd = new LoadMetadata(CatalogueRepository);

            var testDirHelper = new TestDirectoryHelper(GetType());
            testDirHelper.SetUp();

            var projDir = LoadDirectory.CreateDirectoryStructure(testDirHelper.Directory, "Test", true);
            lmd.LocationOfFlatFiles = projDir.RootPath.FullName;
            lmd.SaveToDatabase();
            
            var lp = MockRepository.GenerateMock<ILoadProgress>();
            lp.Expect(m => m.LoadMetadata).Return(lmd);

            cp.Expect(m => m.LoadProgress).Return(lp);
            
            var provider = MockRepository.GenerateMock<ICacheFetchRequestProvider>();

            var useCase = new CachingPipelineUseCase(cp, true, provider);
            var cacheContext = (DataFlowPipelineContext<ICacheChunk>)useCase.GetContext();

            //we shouldn't be able to have data export sources in this context
            Assert.IsTrue(cacheContext.IsAllowable(typeof(SCIStoreWebServiceSource)));
        }
    }
}