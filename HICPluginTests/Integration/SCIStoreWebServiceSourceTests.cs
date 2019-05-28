using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using NUnit.Framework;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.Caching.Requests.FetchRequestProvider;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using SCIStorePlugin.Cache.Pipeline;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;
using Tests.Common;

namespace SCIStorePluginTests.Integration
{
    public class SCIStoreWebServiceSourceTests : DatabaseTests
    {
        [Test]
        [Ignore("Can't get this to work on Jenkins because the configuration file is not being read correctly in that environment.")]
        public void CheckerTest_InvalidConfiguration()
        {
            var component = new SCIStoreWebServiceSource
            {
                Configuration = new WebServiceConfiguration(CatalogueRepository)
                {
                    Endpoint = "foo"
                },
                HealthBoard = HealthBoard.T,
                Discipline = Discipline.Immunology,
            };

            var checkNotifier = MockRepository.GenerateMock<ICheckNotifier>();

            component.Check(checkNotifier);

            var args = checkNotifier.GetArgumentsForCallsMadeOn(
            notifier => notifier.OnCheckPerformed(Arg<CheckEventArgs>.Is.Anything),
            options => options.IgnoreArguments());

            var checkArgs = args[0][0] as CheckEventArgs;
            if (checkArgs.Ex != null)
                Console.WriteLine(ExceptionHelper.ExceptionToListOfInnerMessages(checkArgs.Ex));

            Assert.AreEqual("Could not create the web service repository object", checkArgs.Message);
            Assert.AreEqual(CheckResult.Fail, checkArgs.Result);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void CacheFetchFailureIsRecordedSuccessfully(bool auditAsFailure)
        {
            LoadMetadata loadMetadata = null;
            LoadProgress loadSchedule = null;
            ICacheProgress cacheProgress = null;

            try
            {
                using (var con = CatalogueRepository.GetConnection())
                {
                    var cmd = DatabaseCommandHelper.GetCommand("DELETE FROM CacheFetchFailure", con.Connection, con.Transaction);
                    cmd.ExecuteNonQuery();
                }

                // create entities in database
                loadMetadata = new LoadMetadata(CatalogueRepository);
                loadSchedule = new LoadProgress(CatalogueRepository, loadMetadata);
                cacheProgress = new CacheProgress(CatalogueRepository, loadSchedule);

                // set up the request provider to return a specific CacheFetchRequest instance
                var cacheFetchRequest = new CacheFetchRequest(CatalogueRepository, new DateTime(2015, 1, 1))
                {
                    ChunkPeriod = new TimeSpan(1, 0, 0),
                    CacheProgress = cacheProgress,
                    PermissionWindow = new SpontaneouslyInventedPermissionWindow(cacheProgress)
                };

                var requestProvider = MockRepository.GenerateStub<ICacheFetchRequestProvider>();
                requestProvider.Stub(provider => provider.GetNext(Arg<IDataLoadEventListener>.Is.Anything)).Return(cacheFetchRequest);

                // Create a stubbed retry strategy which will fail and throw the 'DownloadRequestFailedException'
                var failStrategy = MockRepository.GenerateStub<IRetryStrategy>();
                var faultException = new FaultException("Error on the server", new FaultCode("Fault Code"), "Action");
                var downloadException = new DownloadRequestFailedException(cacheFetchRequest.Start, cacheFetchRequest.ChunkPeriod, faultException);
                failStrategy.Stub(
                    strategy =>
                        strategy.Fetch(Arg<DateTime>.Is.Anything, Arg<TimeSpan>.Is.Anything,
                            Arg<IDataLoadEventListener>.Is.Anything, Arg<GracefulCancellationToken>.Is.Anything))
                    .Throw(downloadException);
                failStrategy.WebService =
                    MockRepository.GenerateStub<IRepositorySupportsDateRangeQueries<CombinedReportData>>();

                // Create the source
                var source = new SCIStoreWebServiceSource() { PermissionWindow = new SpontaneouslyInventedPermissionWindow(cacheProgress) };
                source.RequestProvider = requestProvider;

                source.NumberOfTimesToRetry = 1;
                source.NumberOfSecondsToWaitBetweenRetries = "1";
                source.AuditFailureAndMoveOn = auditAsFailure;

                // todo: why does the source need this if it is in the CacheFetchRequest object?
                source.Downloader =
                    MockRepository.GenerateStub<IRepositorySupportsDateRangeQueries<CombinedReportData>>();
                
                source.SetPrivateVariableRetryStrategy_NunitOnly(failStrategy);
               

                // Create the cancellation token and ask the source for a chunk
                var stopTokenSource = new CancellationTokenSource();
                var abortTokenSource = new CancellationTokenSource();
                var token = new GracefulCancellationToken(stopTokenSource.Token, abortTokenSource.Token);


                SCIStoreCacheChunk chunk;

                if (auditAsFailure)
                    chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), token);
                else
                {
                    Assert.Throws<DownloadRequestFailedException>(
                        () => source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), token));
                    return;
                }
                
                Assert.IsNotNull(chunk);
                Assert.AreEqual(downloadException, chunk.DownloadRequestFailedException);

                var failures = CatalogueRepository.GetAllObjects<CacheFetchFailure>();
                var numFailures = failures.Count();
                Assert.AreEqual(1, numFailures, "The cache fetch failure was not recorded correctly.");

                var failure = failures[0];
                Assert.AreEqual(cacheFetchRequest.Start, failure.FetchRequestStart);
            }
            catch (Exception e)
            {
                Assert.Fail(ExceptionHelper.ExceptionToListOfInnerMessages(e, true));
            }
            finally
            {
                if (cacheProgress != null) cacheProgress.DeleteInDatabase();
                if (loadSchedule != null) loadSchedule.DeleteInDatabase();
                if (loadMetadata != null) loadMetadata.DeleteInDatabase();
            }
        }
    }

    internal class AsyncRepositoryTest : IRepositorySupportsDateRangeQueries<CombinedReportData>
    {
        private bool _abort;
        private bool _stop;

        public IEnumerable<CombinedReportData> ReadAll()
        {
            throw new NotImplementedException();
        }

        public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CombinedReportData> ReadSince(DateTime day)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<CombinedReportData>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CombinedReportData> ReadForIntervalAsync(DateTime day, TimeSpan timeSpan)
        {
            while (_abort || _stop)
                Thread.Sleep(100);

            if (_abort || _stop) return null;

            return new List<CombinedReportData>();
        }

        public void Abort()
        {
            _abort = true;
        }

        public void Stop()
        {
            _stop = true;
        }

        public IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan, IDataLoadEventListener listener, GracefulCancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
