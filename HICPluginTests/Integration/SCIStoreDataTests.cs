using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Validation.Constraints.Secondary;
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

        var readCodeConstraint = new Mock<ReferentialIntegrityConstraint>();
        readCodeConstraint.Setup(
                c => c.Validate(It.IsAny<object>(), It.IsAny<object[]>(), It.IsAny<string[]>()))
            .Returns(value:null);

        var reportFactory = new SciStoreReportFactory(readCodeConstraint.Object);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.AreEqual(7, report.Samples.Count);

        var totalNumResults = report.Samples.Aggregate(0, (s, n) => s + n.Results.Count);
        Assert.AreEqual(21, totalNumResults);

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

        var readCodeConstraint = new Mock<ReferentialIntegrityConstraint>();
        readCodeConstraint.Setup(
                c => c.Validate(It.IsAny<object>(), It.IsAny<object[]>(), It.IsAny<string[]>()))
            .Returns(value:null);

        var reportFactory = new SciStoreReportFactory(readCodeConstraint.Object);
        var report = reportFactory.Create(data, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.AreEqual(7, report.Samples.Count);

        var totalNumResults = report.Samples.Aggregate(0, (s, n) => s + n.Results.Count);
        Assert.AreEqual(21, totalNumResults);

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
            
        var totalNumResultsAfterResolvingArtificiallyCreatedDuplication = report.Samples.Aggregate(0, (s, n) => s + n.Results.Count);
        Assert.AreEqual(7, totalNumResultsAfterResolvingArtificiallyCreatedDuplication);


    }
}