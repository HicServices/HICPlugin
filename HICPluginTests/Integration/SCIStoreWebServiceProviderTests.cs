using System.IO;
using System.Xml.Serialization;
using HICPluginTests;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Secondary;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

[Category("Database")]
public class SCIStoreWebServiceProviderTests : DatabaseTests
{
    [Test]
    public void LabWithDifferentClinicalCodeDescriptionsForSameTestCode()
    {
        Validator.LocatorForXMLDeserialization = RepositoryLocator;

        var serializer = new XmlSerializer(typeof(CombinedReportData));
        var lab = serializer.Deserialize(new StringReader(TestReports.report_with_multiple_descriptions)) as CombinedReportData;

        var readCodeConstraint = new Mock<ReferentialIntegrityConstraint>();
        readCodeConstraint.Setup(
                c => c.Validate(It.IsAny<object>(), It.IsAny<object[]>(), It.IsAny<string[]>()))
            .Returns(value:null);

        var reportFactory = new SciStoreReportFactory(readCodeConstraint.Object);
        var report = reportFactory.Create(lab, new ThrowImmediatelyDataLoadEventListener());
    }
}