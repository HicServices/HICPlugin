using System;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace SCIStorePluginTests.Integration
{
    public class SCIStoreWebServiceDownloadTests : DatabaseTests
    {
        private WebServiceConfiguration _validTaysideConfiguration;
        private WebServiceConfiguration _validFifeConfiguration;

        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            TestCredentialHelper credentialHelper = null;
            try
            {
                credentialHelper = new TestCredentialHelper();
            }
            catch (CredentialsNotAvailableException e)
            {
                Assert.Ignore("Could not load credentials for Tayside web service (this may be intended if the web service is not reachable from the test machine, e.g. Jenkins): {0}" + Environment.NewLine + "{1}",
                    e.Message, ExceptionHelper.ExceptionToListOfInnerMessages(e));
            }

            TestCredential taysideCredentials = null;
            try
            {
                taysideCredentials = credentialHelper.GetCredentials("Tayside");
            }
            catch (Exception)
            {
                Assert.Ignore("Could not retrieve credentials for Tayside (machine needs to be on NHS network in order to run these tests)");
            }

            _validTaysideConfiguration = new WebServiceConfiguration(CatalogueRepository)
            {
                Endpoint = taysideCredentials.Endpoint,
                Username = taysideCredentials.Username,
                Password = taysideCredentials.Password,
                MaxBufferSize = 500000000,
                MaxReceivedMessageSize = 500000000
            };

            var fifeCredentials = credentialHelper.GetCredentials("Fife");
            _validFifeConfiguration = new WebServiceConfiguration(CatalogueRepository)
            {
                Endpoint = fifeCredentials.Endpoint,
                Username = fifeCredentials.Username,
                Password = fifeCredentials.Password,
                MaxBufferSize = 500000000,
                MaxReceivedMessageSize = 500000000
            };
        }

        /// <summary>
        /// Tests the component's check interface, which checks if the web service can be contacted
        /// </summary>
        [Test, Category("NHSNetworkOnly")]
        public void CheckerTest_ValidConfiguration()
        {
            var component = new SCIStoreWebServiceSource
            {
                Configuration = _validTaysideConfiguration,
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Immunology
            };

            var checkNotifier = MockRepository.GenerateMock<ICheckNotifier>();
            component.Check(checkNotifier);

            var args = checkNotifier.GetArgumentsForCallsMadeOn(
                notifier => notifier.OnCheckPerformed(Arg<CheckEventArgs>.Is.Anything),
                options => options.IgnoreArguments());

            
            var checkArgs = args[0][0] as CheckEventArgs;
            Assert.AreEqual("Web Service Connection to " + _validTaysideConfiguration.Endpoint + " is available", checkArgs.Message);
        }

        /// <summary>
        /// Attempts to download some data from Tayside and sanity checks the cache chunk that the source component spits out
        /// </summary>
        [Test, Category("NHSNetworkOnly")]
        public void CheckTaysideDataDownload()
        {
            var endDate = DateTime.Now.AddDays(-30);
            var startDate = endDate.AddHours(-1);

            var lmd = new LoadMetadata(CatalogueRepository);
            var loadProgress = new LoadProgress(CatalogueRepository, lmd);
            var cacheProgress = new CacheProgress(CatalogueRepository, loadProgress) {CacheLagPeriod = "14d"};
            cacheProgress.SaveToDatabase();

            try
            {
                var permissionWindow = MockRepository.GenerateStub<IPermissionWindow>();
                permissionWindow.Stub(window => window.WithinPermissionWindow()).Return(true);

                var component = new SCIStoreWebServiceSource
                {
                    Configuration = _validTaysideConfiguration,
                    HealthBoard = HealthBoard.T,
                    Discipline = Discipline.Biochemistry,
                    RequestProvider = new CacheFetchRequestProvider(new CacheFetchRequest(CatalogueRepository, startDate)
                    {
                        ChunkPeriod = new TimeSpan(1, 0, 0),
                        CacheProgress = cacheProgress
                    }),
                    PermissionWindow = permissionWindow
                };

                var cacheChunk = component.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

                Assert.IsNotNull(cacheChunk);
                Assert.IsNotEmpty(cacheChunk.CombinedReports);
                Assert.AreEqual(startDate, cacheChunk.FetchDate);
            }
            finally
            {
                cacheProgress.DeleteInDatabase();
                loadProgress.DeleteInDatabase();
                lmd.DeleteInDatabase();
            }

        }

        /// <summary>
        /// Attempts to download some data from Tayside and sanity checks the cache chunk that the source component spits out
        /// </summary>
        [Test, Category("NHSNetworkOnly")]
        public void CheckFifePathologyDataDownload()
        {
            //var endDate = DateTime.Now.AddDays(-30);
            var endDate = new DateTime(2008, 7, 1, 12, 0, 0);
            var startDate = endDate.AddHours(-1);

            var cacheProgress = MockRepository.GenerateStub<ICacheProgress>();
            cacheProgress.Stub(progress => progress.GetCacheLagPeriod()).Return(new CacheLagPeriod(14, CacheLagPeriod.PeriodType.Day));

            var permissionWindow = MockRepository.GenerateStub<IPermissionWindow>();
            permissionWindow.Stub(window => window.WithinPermissionWindow()).Return(true);

            var component = new SCIStoreWebServiceSource
            {
                Configuration = _validFifeConfiguration,
                HealthBoard = HealthBoard.F,
                Discipline = Discipline.Pathology,
                RequestProvider = new CacheFetchRequestProvider(new CacheFetchRequest(CatalogueRepository, startDate)
                {
                    ChunkPeriod = new TimeSpan(8, 0, 0),
                    CacheProgress = cacheProgress
                }),
                PermissionWindow = permissionWindow
            };

            var cacheChunk = component.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.IsNotEmpty(cacheChunk.CombinedReports);
            Assert.AreEqual(startDate, cacheChunk.FetchDate);
        }
    }
}