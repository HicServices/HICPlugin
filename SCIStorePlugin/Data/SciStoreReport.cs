using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.Validation.Constraints.Secondary;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Data;

public class SciStoreReport
{
    public SciStoreHeader Header { get; set; }
    public HashSet<SciStoreSample> Samples { get; set; }

    public SciStoreReport()
    {
        // For XML serialiser
    }

    public SciStoreReport(SciStoreReport report)
    {
        Header = report.Header;
        Samples = report.Samples;
    }
}

public class BadCombinedReportDataException : Exception
{
    public object Sender { get; private set; }
    public CombinedReportData BadReport { get; private set; }

    public BadCombinedReportDataException(object sender, CombinedReportData badReport, Exception ex) : base ("", ex)
    {
        Sender = sender;
        BadReport = badReport;
    }

    public override string ToString()
    {
        return
            $"Bad report: {BadReport.SciStoreRecord.LabNumber}, {BadReport.SciStoreRecord.TestReportID} ({BadReport.HbExtract})";
    }
}

public class SciStoreReportFactory
{
    private readonly ReferentialIntegrityConstraint _readCodeConstraint;
    public bool IgnoreBadData { get; set; }

    public SciStoreReportFactory(ReferentialIntegrityConstraint readCodeConstraint)
    {
        _readCodeConstraint = readCodeConstraint;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="combinedReport"></param>
    /// <param name="listener"></param>
    /// <returns></returns>
    /// <exception cref="BadCombinedReportDataException">False to issue an Exception, True to issue warning to <paramref name="listener"/> and return null </exception>
    public SciStoreReport Create(CombinedReportData combinedReport,IDataLoadEventListener listener)
    {   
        var sampleFactory = new SciStoreSampleFactory(_readCodeConstraint);

        try
        {
            var report = new SciStoreReport
            {
                Header = SciStoreHeaderFactory.Create(combinedReport),
                Samples = new HashSet<SciStoreSample>()
            };

            // bail out if we don't see any samples in this report
            var serviceResults = combinedReport.GetServiceResults();
            if (serviceResults == null || !serviceResults.Any())
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                    $"No samples found in report {report.Header.LabNumber}-{report.Header.TestReportID}"));
                return report;
            }

            foreach (var serviceResult in combinedReport.GetServiceResults())
            foreach (var testResultSet in serviceResult.TestResultSets)
                report.Samples.Add(sampleFactory.Create(report.Header, serviceResult, testResultSet, listener));

            return report;
        }
        catch (Exception e)
        {
            //if we are not ignoring bad data throw
            if (!IgnoreBadData)
                throw new BadCombinedReportDataException(this, combinedReport, e);

            //we are ignoring bad data
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Ignoring bad data", e));
            return null;
        }
    }

}