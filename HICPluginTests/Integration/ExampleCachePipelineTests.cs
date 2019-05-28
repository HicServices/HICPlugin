using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace SCIStorePluginTests.Integration
{
    public class TestCredential
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Endpoint { get; set; }
    }

    internal class CredentialsNotAvailableException : Exception
    {
        public CredentialsNotAvailableException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class TestCredentialHelper
    {
        private readonly Dictionary<string, TestCredential> _credentials;
        
        public TestCredentialHelper()
        {
            string credentialsPath;
            try
            {
                var configSection = ((ConnectionStringsSection) ConfigurationManager.GetSection("connectionStrings"));
                credentialsPath = configSection.ConnectionStrings["CredentialsFilePath"].ConnectionString;
            }
            catch (ConfigurationErrorsException e)
            {
                throw new CredentialsNotAvailableException("Could not load credentials information from connections.config", e);
            }

            if (string.IsNullOrEmpty(credentialsPath))
                throw new CredentialsNotAvailableException("No credentials file specified.");

            if (!File.Exists(credentialsPath))
                throw new CredentialsNotAvailableException("Credentials file not found: " + credentialsPath);

            var rows = File.ReadLines(credentialsPath).ToList();

            _credentials = new Dictionary<string, TestCredential>
            {
                {"Tayside", ParseCredentials(rows[0])},
                {"Fife", ParseCredentials(rows[1])}
            };
        }

        private TestCredential ParseCredentials(string row)
        {
            var items = row.Split(',');
            return new TestCredential
            {
                Endpoint = items[0],
                Username = items[1],
                Password = items[2]
            };
        }

        public TestCredential GetCredentials(string key)
        {
            if (!_credentials.ContainsKey(key))
                throw new InvalidOperationException("There are no credentials for key: " + key);
            return _credentials[key];
        }
    }

    public class ExampleCachePipelineTests : DatabaseTests
    {
        private TestDirectoryHelper _directoryHelper;
        private TestCredentialHelper _credentialHelper;
        private WebServiceConfiguration _validTaysideConfiguration;
        private CredentialsNotAvailableException _credentialsException;

        [OneTimeSetUp]
        protected override void SetUp()
        {
            base.SetUp();

            _directoryHelper = new TestDirectoryHelper(GetType());
            _directoryHelper.SetUp();

            try
            {
                _credentialHelper = new TestCredentialHelper();
                var credentials = _credentialHelper.GetCredentials("Tayside");
                _validTaysideConfiguration = new WebServiceConfiguration(CatalogueRepository)
                {
                    Endpoint = credentials.Endpoint,
                    Username = credentials.Username,
                    Password = credentials.Password,
                    MaxBufferSize = 20000000,
                    MaxReceivedMessageSize = 20000000
                };
            }
            catch (CredentialsNotAvailableException e)
            {
                _credentialsException = e;
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _directoryHelper.TearDown();
        }

        [SetUp]
        public void BeforeEachTest()
        {
            if (_credentialsException != null)
                Assert.Ignore("Could not load credentials for Tayside web service (this may be intended if the web service is not reachable from the test machine, e.g. Jenkins): {0}" + Environment.NewLine + "{1}", 
                    _credentialsException.Message, ExceptionHelper.ExceptionToListOfInnerMessages(_credentialsException));
        }

        /// <summary>
        /// Basic test of Biochemistry caching using in-memory source and destination objects, downloads an hour of results and checks that an
        /// appropriately named archive is present after the download.
        /// </summary>
        [Test]
        public void TestWebServicePipelineInMemory()
        {
            var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("TestWebServicePipeline");

            var startDate = new DateTime(2015, 8, 1, 13, 0, 0);
            var endDate = new DateTime(2015, 8, 1, 14, 0, 0); // end date is open interval (i.e. strictly less than)
            IDataFlowSource<ICacheChunk> source = new SCIStoreWebServiceSource
            {
                Configuration = _validTaysideConfiguration,
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Biochemistry,
                NumberOfTimesToRetry = 2,
                NumberOfSecondsToWaitBetweenRetries = "30,60"
            };

            IDataFlowDestination<ICacheChunk> destination = new SCIStoreCacheDestination
            {
                HealthBoard = HealthBoard.T,
                Discipline =  Discipline.Biochemistry,
                SilentRunning = true
            };

            var request = MockRepository.GenerateStub<ICacheFetchRequest>();
            request.Start = startDate;
            request.ChunkPeriod = new TimeSpan(1, 0, 0);
            request.CacheProgress = MockRepository.GenerateStub<ICacheProgress>();
            request.CacheProgress.CacheLagPeriod = "14d";
            var requestProvider = new CacheFetchRequestProvider(request);

            var permissionWindow = MockRepository.GenerateStub<IPermissionWindow>();
            permissionWindow.Stub(window => window.WithinPermissionWindow()).Return(true);
            permissionWindow.RequiresSynchronousAccess = true;

            var hicProjectDirectory = LoadDirectory.CreateDirectoryStructure(rootDirectory, "SCIStore");

            var cp = MockRepository.GenerateMock<CacheProgress>();


            var engine = new DataFlowPipelineEngine<ICacheChunk>((DataFlowPipelineContext<ICacheChunk>) new CachingPipelineUseCase(cp).GetContext(), 
                source, destination, new ThrowImmediatelyDataLoadEventListener());
            engine.Initialize(requestProvider, permissionWindow, hicProjectDirectory);
            engine.ExecutePipeline(new GracefulCancellationToken());

            var downloadDir = Path.Combine(hicProjectDirectory.Cache.FullName, "T", "Biochemistry");
            var expectedArchiveFilepath = Path.Combine(downloadDir, "2015-08-01.zip");
            Assert.IsTrue(File.Exists(expectedArchiveFilepath), "The archive has not been created");

            // make sure that the archiver has cleaned up after itself
            Assert.IsEmpty(Directory.EnumerateFiles(downloadDir, "*.xml"), "The process has not cleaned up the temporary cache files");
        }

        /// <summary>
        /// Basic test of Biochemistry caching using in-memory source and destination objects, downloads an hour of results and checks that an
        /// appropriately named archive is present after the download.
        /// </summary>
        [Test]
        public void TestWebServicePipelineInMemoryWithDownloadError()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var rootDirectory = tempDir.EnumerateDirectories().FirstOrDefault(info => info.Name.Equals("SCIStoreIntegration"));
            if (rootDirectory != null)
                rootDirectory.Delete(true);

            rootDirectory = tempDir.CreateSubdirectory("SCIStoreIntegration");

            var startDate = new DateTime(2015, 8, 1, 13, 0, 0);
            var endDate = new DateTime(2015, 8, 1, 14, 0, 0); // end date is open interval (i.e. strictly less than)
            
            var retryStrategy = MockRepository.GenerateStub<IRetryStrategy>();
            retryStrategy.Stub(strategy => strategy.Fetch(Arg<DateTime>.Is.Anything, Arg<TimeSpan>.Is.Anything, Arg<IDataLoadEventListener>.Is.Anything, Arg<GracefulCancellationToken>.Is.Anything))
                .Throw(new DownloadRequestFailedException(startDate, new TimeSpan(1, 0, 0, 0), new Exception("Test")));

            var source = new SCIStoreWebServiceSource
            {
                Configuration = _validTaysideConfiguration,
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Biochemistry,
                NumberOfTimesToRetry = 2,
                NumberOfSecondsToWaitBetweenRetries = "30,60"
            };

            var destination = new SCIStoreCacheDestination
            {
                HealthBoard = HealthBoard.T,
                Discipline =  Discipline.Biochemistry,
                SilentRunning = true
            };

            var request = MockRepository.GenerateStub<ICacheFetchRequest>();
            request.Start = startDate;
            request.ChunkPeriod = new TimeSpan(1, 0, 0);
            request.CacheProgress = MockRepository.GenerateStub<ICacheProgress>();
            request.CacheProgress.CacheLagPeriod = "14d";
            var requestProvider = new CacheFetchRequestProvider(request);

            var permissionWindow = MockRepository.GenerateStub<IPermissionWindow>();
            permissionWindow.Stub(window => window.WithinPermissionWindow()).Return(true);
            permissionWindow.RequiresSynchronousAccess = false;



            var cp = MockRepository.GenerateMock<CacheProgress>();

            var useCase = new CachingPipelineUseCase(cp);
            

            var hicDirectory = LoadDirectory.CreateDirectoryStructure(rootDirectory, "SCIStore");
            var engine = new DataFlowPipelineEngine<ICacheChunk>((DataFlowPipelineContext<ICacheChunk>) useCase.GetContext(), source, destination, new ThrowImmediatelyDataLoadEventListener());
            engine.Initialize(requestProvider, permissionWindow, hicDirectory);
            engine.ExecutePipeline(new GracefulCancellationToken());

            var expectedErrorDirectory = Path.Combine(hicDirectory.Cache.FullName, "Errors", "T", "Biochemistry");
            Assert.IsTrue(Directory.Exists(expectedErrorDirectory), "The error directory has not been created");

            var expectedErrorFilename = Path.Combine(expectedErrorDirectory, "2015-08-01 130000.txt");
            Assert.IsTrue(File.Exists(expectedErrorFilename), "The error file has not been created");

            var errorFileContents = File.ReadAllLines(expectedErrorFilename);
            Assert.AreEqual("Failed to download data requested for 01/08/2015 13:00:00 (interval 1.00:00:00)",
                errorFileContents[0], "The error file does not contain the expected summary text");
            Assert.AreEqual("Test", errorFileContents[1], "The error file does not contain the expected exception text");

            rootDirectory.Delete(true);
        }

        /// <summary>
        /// Test full SCIStore caching pipeline using database entities
        /// </summary>
        [Test]
        public void TestWebServicePipelineFromDatabase()
        {
            var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("TestWebServicePipelineFromDatabase");
            var hicProjectDirectory = LoadDirectory.CreateDirectoryStructure(rootDirectory, "Test");
            Pipeline pipeline = null;
            try
            {
                var startingCacheFillProgress = new DateTime(2015, 8, 1, 12, 0, 0);

                int cacheProgressID;
                pipeline = SetUpDatabaseObjects(rootDirectory, startingCacheFillProgress, out cacheProgressID);

                var cacheProgress = CatalogueRepository.GetObjectByID<CacheProgress>(cacheProgressID);
                var engine = CreateEngineFromCacheProgress(cacheProgress, hicProjectDirectory);

                engine.ExecutePipeline(new GracefulCancellationToken());

                var downloadDir = Path.Combine(rootDirectory.FullName, "T", "Biochemistry");
                var expectedArchiveFilepath = Path.Combine(downloadDir, "2015-08-01.zip");
                Assert.IsTrue(File.Exists(expectedArchiveFilepath));

                // make sure that the archiver has cleaned up after itself
                Assert.IsEmpty(Directory.EnumerateFiles(downloadDir, "*.xml"));

                var updatedCacheProgress = CatalogueRepository.GetObjectByID<CacheProgress>(cacheProgress.ID);
                var expectedNewCacheFillProgress = startingCacheFillProgress.Add(updatedCacheProgress.ChunkPeriod);
                Assert.AreEqual(expectedNewCacheFillProgress, updatedCacheProgress.CacheFillProgress);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (pipeline != null)
                    pipeline.DeleteInDatabase();
            }
        }

        private Pipeline SetUpDatabaseObjects(DirectoryInfo rootDirectory, DateTime startingCacheFillProgress, out int cacheProgressID)
        {
            Pipeline pipeline = null;
            cacheProgressID = 0;
            try
            {
                // set up objects in database
                pipeline = new Pipeline(CatalogueRepository, "TestSCIStoreWebService");
                var sourceComponent = new PipelineComponent(CatalogueRepository, pipeline, typeof (SCIStoreWebServiceSource), 0, "Web Service downloader");
                var destinationComponent = new PipelineComponent(CatalogueRepository, pipeline, typeof (SCIStoreCacheDestination), 0, "Filesystem cache");

                pipeline.SourcePipelineComponent_ID = sourceComponent.ID;
                pipeline.DestinationPipelineComponent_ID = destinationComponent.ID;
                pipeline.SaveToDatabase();

                // set up argument entities in database for DemandsInitialization fields
                var sourceArgs = sourceComponent.CreateArgumentsForClassIfNotExists<SCIStoreWebServiceSource>().ToList();
                sourceArgs.Single(argument => argument.Name == "Configuration").SetValue(_validTaysideConfiguration);
                sourceArgs.Single(argument => argument.Name == "HealthBoard").SetValue(HealthBoard.T);
                sourceArgs.Single(argument => argument.Name == "Discipline").SetValue(Discipline.Biochemistry);
                sourceArgs.ForEach(argument => argument.SaveToDatabase());

                var destinationArgs = destinationComponent.CreateArgumentsForClassIfNotExists<SCIStoreCacheDestination>()
                        .ToList();
                destinationArgs.Single(argument => argument.Name == "DateFormat").SetValue("yyyy-MM-dd");
                destinationArgs.Single(argument => argument.Name == "ArchiveType").SetValue(CacheArchiveType.Zip);
                destinationArgs.Single(argument => argument.Name == "CacheDirectory").SetValue(rootDirectory);
                destinationArgs.Single(argument => argument.Name == "CacheLayoutType").SetValue("SCIStore.Cache.SciStoreCacheLayout");
                destinationArgs.ForEach(argument => argument.SaveToDatabase());

                // set up load schedule and cache progress
                var loadMetadata = new LoadMetadata(CatalogueRepository);
                var loadSchedule = new LoadProgress(CatalogueRepository, loadMetadata);
                var cacheProgress = new CacheProgress(CatalogueRepository, loadSchedule)
                {
                    CacheFillProgress = startingCacheFillProgress,
                    CacheLagPeriod = "14d",
                    ChunkPeriod = new TimeSpan(0, 30, 0),
                    Pipeline_ID = pipeline.ID
                };
                cacheProgress.SaveToDatabase();
                
                cacheProgressID = cacheProgress.ID;
                
                return pipeline;
            }
            catch (Exception)
            {
                if (pipeline != null)
                    pipeline.DeleteInDatabase();

                throw;
            }
        }

        private CacheFetchRequest CreateCacheFetchRequest(ICacheProgress cacheProgress, DateTime? endDateOverride = null)
        {
            var startDate = cacheProgress.CacheFillProgress;
            if (startDate == null)
            {
                // get the origin date from the LoadSchedule
                var loadSchedule = CatalogueRepository.GetObjectByID<LoadProgress>(cacheProgress.LoadProgress_ID);
                var originDate = loadSchedule.OriginDate;
                if (originDate == null)
                    throw new Exception("No progress and no origin date: don't know when to begin caching from!");

                startDate = originDate.Value;
            }

            //var endDate = endDateOverride == null
            //    ? cacheProgress.GetCacheLagPeriod().CalculateStartOfLagPeriodFrom(DateTime.Now)
            //    : endDateOverride.Value;

            return new CacheFetchRequest(CatalogueRepository, startDate.Value)
            {
                ChunkPeriod = cacheProgress.ChunkPeriod,
                CacheProgress = cacheProgress
            };
        }

        private IDataFlowPipelineEngine CreateEngineFromCacheProgress(ICacheProgress cacheProgress, ILoadDirectory hicProjectDirectory)
        {
            if (cacheProgress.Pipeline_ID == null)
                throw new Exception("CacheProgress " + cacheProgress.ID + " is not configured with a pipeline");

            // create engine
            var useCase = new CachingPipelineUseCase(cacheProgress);
            
            var engine = useCase.GetEngine(new ThrowImmediatelyDataLoadEventListener());
            
            // create fetch request from CacheProgress
            var fetchRequest = CreateCacheFetchRequest(cacheProgress);
            fetchRequest.ChunkPeriod = new TimeSpan(0, 30, 0);

            var fetchRequestProvider = new CacheFetchRequestProvider(fetchRequest);
            engine.Initialize(fetchRequestProvider, new SpontaneouslyInventedPermissionWindow(cacheProgress), hicProjectDirectory);
            
            return engine;
        }

        [Test]
        [Ignore("Refactor PeriodicRetrieverFactory out of this test")]
        public void TestPipelineWithCachingHost()
        {
            var rootDirectory = _directoryHelper.Directory.CreateSubdirectory("TestWebServicePipelineFromDatabase");

            Pipeline pipeline = null;
            ICacheProgress cacheProgress = null;
            IPermissionWindow permissionWindow = null;
            try
            {
                var startingCacheFillProgress = new DateTime(2015, 6, 1, 12, 0, 0);

                int cacheProgressID;
                pipeline = SetUpDatabaseObjects(rootDirectory, startingCacheFillProgress, out cacheProgressID);

                // we now have single LoadMetadata, LoadSchedule and CacheProgress objects in the database
                // add a permission window
                permissionWindow = new PermissionWindow(CatalogueRepository);

                cacheProgress = CatalogueRepository.GetObjectByID<CacheProgress>(cacheProgressID);
                cacheProgress.PermissionWindow_ID = permissionWindow.ID;
                cacheProgress.SaveToDatabase();

                /*
                var cachingHost = new CachingHost(new PeriodicRetrieverFactory(), new DynamicPipelineEngineFactory())
                {
                    CacheProgressList = new List<ICacheProgress> {cacheProgress}
                };

                var complete = false;
                var task = Task.Run(() =>
                {
                    cachingHost.Start(new ToConsoleDataLoadEventReciever(), new GracefulCancellationToken());
                    complete = true;
                });
                 * */
                //task.Wait(15000);
                //Thread.Sleep(7000);
                //cachingHost.Abort();
                //while (!complete)
                //    Thread.Sleep(100);
                //task.Wait();
            }
            finally
            {
                if (pipeline != null)
                    pipeline.DeleteInDatabase();

                if (cacheProgress != null)
                {
                    var loadProgress = CatalogueRepository.GetObjectByID<LoadProgress>(cacheProgress.LoadProgress_ID);
                    cacheProgress.DeleteInDatabase();
                    loadProgress.DeleteInDatabase();
                }

                if (permissionWindow != null)
                    permissionWindow.DeleteInDatabase();
            }
        }

        public class SCIStoreCacheSourceTest : SCIStoreWebServiceSource
        {
            private readonly SCIStoreCacheChunk _chunk;
            private bool _firstTime = true;

            public SCIStoreCacheSourceTest(SCIStoreCacheChunk chunk)
            {
                _chunk = chunk;
            }

            public override SCIStoreCacheChunk GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
            {
                if (_firstTime)
                {
                    _firstTime = false;
                    return _chunk;
                }

                return null;
            }
        }

        public class SCIStoreCacheDestinationTest : SCIStoreCacheDestination
        {
            private readonly SCIStoreCacheChunk _chunk;

            public SCIStoreCacheDestinationTest(SCIStoreCacheChunk chunk)
            {
                _chunk = chunk;
            }


            public override SCIStoreCacheChunk ProcessPipelineData(SCIStoreCacheChunk toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
            {
                Debug.Assert(toProcess == _chunk);
                return null;
            }
        }
    }
}
