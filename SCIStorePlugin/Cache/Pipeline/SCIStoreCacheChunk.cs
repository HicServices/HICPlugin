using System;
using Rdmp.Core.Caching.Requests;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;

namespace SCIStorePlugin.Cache.Pipeline
{
    public class SCIStoreCacheChunk : ICacheChunk
    {
        public ICacheFetchRequest Request { get; private set; }

        public CombinedReportData[] CombinedReports { get; private set; } // for the time being as there is a repository for writing these out as XML
        public DateTime FetchDate { get; private set; }
        public HealthBoard HealthBoard { get; set; }
        public Discipline Discipline { get; set; }
        public DownloadRequestFailedException DownloadRequestFailedException { get; set; }

        public SCIStoreCacheChunk(CombinedReportData[] combinedReports, DateTime fetchDate, ICacheFetchRequest request)
        {
            CombinedReports = combinedReports;
            FetchDate = fetchDate;
            Request = request;
        }

        
    }
}