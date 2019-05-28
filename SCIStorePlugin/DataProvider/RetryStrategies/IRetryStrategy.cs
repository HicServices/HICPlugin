using System;
using System.Collections.Generic;
using Rdmp.Core.DataFlowPipeline;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;

namespace SCIStorePlugin.DataProvider.RetryStrategies
{
    public interface IRetryStrategy
    {
        IEnumerable<CombinedReportData> Fetch(DateTime dateToFetch, TimeSpan interval, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken);
        
        IRepositorySupportsDateRangeQueries<CombinedReportData> WebService { get; set; }
    }
}