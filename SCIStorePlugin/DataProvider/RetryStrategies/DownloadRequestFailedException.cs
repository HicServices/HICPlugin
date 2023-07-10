using System;

namespace SCIStorePlugin.DataProvider.RetryStrategies;

public class DownloadRequestFailedException : Exception
{
    public DateTime FetchDate { get; private set; }
    public TimeSpan Interval { get; private set; }

    public DownloadRequestFailedException(DateTime fetchDate, TimeSpan interval, Exception innerException = null)
        : base($"Failed to download data requested for {fetchDate} (interval {interval})", innerException)
    {
        FetchDate = fetchDate;
        Interval = interval;
    }
}