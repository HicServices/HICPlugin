using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using Rdmp.Core.DataFlowPipeline;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;

namespace SCIStorePlugin.DataProvider.RetryStrategies;

public class LimitedRetryThenContinueStrategy : IRetryStrategy
{
    public int NumberOfTimesToRetry { get; set; }
    public List<int> SleepTimeBetweenTries { get; set; }

    private DateTime _dateToFetch;


    /// <summary>
    ///
    /// </summary>
    /// <param name="numberOfTimesToRetry">The total number of times to retry after failing e.g. 3  = make 3 more attempts after the first fails</param>
    /// <param name="sleepTimeBetweenTries">The number of seconds to wait after each try, if there are less elements than the number of attempts then the final index is used for all remaining waits e.g. 3 waits and sleep times 30,60 means waiting for 30s then 60s then 60s</param>
    /// <param name="webService">The web service you are trying to retry</param>
    public LimitedRetryThenContinueStrategy(int numberOfTimesToRetry, List<int> sleepTimeBetweenTries, IRepositorySupportsDateRangeQueries<CombinedReportData> webService)
    {
        NumberOfTimesToRetry = numberOfTimesToRetry;
        SleepTimeBetweenTries = sleepTimeBetweenTries;
        WebService = webService;
    }

    public IRepositorySupportsDateRangeQueries<CombinedReportData> WebService { get; set; }

    public IEnumerable<CombinedReportData> Fetch(DateTime startDate, TimeSpan interval, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        var retryCount = NumberOfTimesToRetry;
            
        _dateToFetch = startDate;

        TryAgain:

        try
        {
            cancellationToken.ThrowIfAbortRequested();

            var reports = WebService.ReadForInterval(startDate, interval, listener, cancellationToken);
            return reports;
        }
        catch (OperationCanceledException)
        {
            // Catching and rethrowing here because we have a Pokemon clause at the end, since we're not sure what other exceptions the Web Service might throw our way
            listener.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Information,
                    "The fetch has been cancelled, nothing more to do here."));
            throw;
        }
        catch (WebServiceRetrievalFailure e)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, $"WebServiceRetrievalFailure: {e}"));
            retryCount = RetryAfterCooldown(interval, listener, retryCount, e);
            goto TryAgain;
        }
        catch (FaultException e)
        {
            var reasonText = e.CreateMessageFault().Reason.GetMatchingTranslation().Text;
            if (reasonText.Contains("Error performing search"))
            {
                // this is a non-recoverable fault, no point in retrying
                throw new DownloadRequestFailedException(_dateToFetch, interval, e);
            }

            retryCount = RetryAfterCooldown(interval, listener, retryCount, e);
            goto TryAgain;
        }
        catch (EndpointNotFoundException e)
        {
            // todo: log this
            // this is recoverable, could be that the Endpoint is temporarily unroutable so retry after cooldown
            retryCount = RetryAfterCooldown(interval, listener, retryCount, e);
            goto TryAgain;
        }
        catch (Exception e)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Exception thrown from WebService.ReadForInterval: {e.GetType().Name}"));
            retryCount = RetryAfterCooldown(interval, listener, retryCount, e);
            goto TryAgain;
        }
    }

    public int RetryAfterCooldown(TimeSpan interval, IDataLoadEventListener listener, int retryCount, Exception e)
    {
        if (retryCount < 0)
            throw new DownloadRequestFailedException(_dateToFetch, interval, e);

        int listIndex = NumberOfTimesToRetry - retryCount;
        if (listIndex >= SleepTimeBetweenTries.Count)
            listIndex = SleepTimeBetweenTries.Count - 1;


        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, e.Message));
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Sleeping for {SleepTimeBetweenTries[listIndex]} seconds..."));
        Thread.Sleep(1000 * SleepTimeBetweenTries[listIndex]); // back off for 30 seconds
            
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Retrying ({retryCount} attempts remaining)..."));
        return retryCount - 1;
    }
}