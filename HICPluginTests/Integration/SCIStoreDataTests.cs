using System.IO;
using System.Linq;
using System.Xml.Serialization;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Integration;


public class SCIStoreDataTests
{
    [Test]
    [Category("Integration")]
    [Ignore("unit-test.xml needs added to project as a resource")]
    public void TestResultsDuplicationInTestSetDetails()
    {
        var testFilePath = @"";
        var xmlSerialiser = new XmlSerializer(typeof (CombinedReportData));
        CombinedReportData data = null;
        using (var fs = new FileStream(testFilePath, FileMode.Open))
        {
            data = (CombinedReportData) xmlSerialiser.Deserialize(fs);
        }

        var readCodeConstraint = new MockReferentialIntegrityConstraint();

        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

       Assert.That(7, Is.EqualTo(report.Samples.Count));

        var totalNumResults = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(21, Is.EqualTo(totalNumResults));

    }


    [Test]
    [Category("Integration")]
    [Ignore("unit-test.xml needs added to project as a resource")]
    public void Test_MultipleTestResultOrdersThatAreTheSame()
    {

        var testFilePath = @"";
        var xmlSerialiser = new XmlSerializer(typeof(CombinedReportData));
        CombinedReportData data = null;
        using (var fs = new FileStream(testFilePath, FileMode.Open))
        {
            data = (CombinedReportData)xmlSerialiser.Deserialize(fs);
        }

        var readCodeConstraint = new MockReferentialIntegrityConstraint();
        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

       Assert.That(7, Is.EqualTo(report.Samples.Count));

        var totalNumResults = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(21, Is.EqualTo(totalNumResults));

        //artificially introduce duplication
        foreach (var sciStoreSample in report.Samples)
        {

            foreach (var sciStoreResult in sciStoreSample.Results)
            {
                sciStoreResult.ClinicalCircumstanceDescription = "Test for fish presence";
                sciStoreResult.TestResultOrder = 0;

            }
            sciStoreSample.ResolveTestResultOrderDuplication();
        }
            
        var totalNumResultsAfterResolvingArtificiallyCreatedDuplication = report.Samples.Aggregate(0, static (s, n) => s + n.Results.Count);
       Assert.That(7, Is.EqualTo(totalNumResultsAfterResolvingArtificiallyCreatedDuplication));


    }
}