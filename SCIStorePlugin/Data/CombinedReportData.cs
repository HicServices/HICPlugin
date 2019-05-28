using System;
using System.Collections.Generic;
using System.Linq;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data
{
    public class CombinedReportData
    {
        public string HbExtract { get; set; } // This field is not in the SciStore data, it is only available at time of extraction
        public SciStoreRecord SciStoreRecord { get; set; }
        public InvestigationReport InvestigationReport { get; set; }

        public TEST_RESULT_TYPE[] GetTestResults()
        {
            var testResults = new List<TEST_RESULT_TYPE>();
            foreach (var result in InvestigationReport.ReportData.ServiceResult)
            {
                foreach (var testSample in result.TestResultSets)
                {
                    testResults.AddRange(testSample.TestResults);
                }
            }

            return testResults.ToArray();
        }

        public CLINICAL_CIRCUMSTANCE_TYPE[] GetSampleTypes()
        {
            var serviceResults = InvestigationReport.ReportData.ServiceResult.ToList();
            
            if (serviceResults.Count == 0)
                throw new Exception("This is unlikely to be valid, but investigate and add logic/remove throw if it is");

            if (serviceResults.Count > 1)
                throw new Exception("This may be valid but the code doesn't deal with this case. Investigate and add logic.");

            var serviceResult = serviceResults[0]; // todo: not sure if this is valid, but handlers above will capture other cases
            return serviceResult.SampleDetails.SampleName;
        }

        public ResultSuite[] GetServiceResults()
        {
            return InvestigationReport.ReportData.ServiceResult;
        }
    }
}