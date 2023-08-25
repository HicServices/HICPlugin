using System.IO;
using System.Xml.Serialization;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Validation;
using Rdmp.Core.ReusableLibraryCode.Progress;
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

        var readCodeConstraint = new MockReferentialIntegrityConstraint();

        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        reportFactory.Create(lab, ThrowImmediatelyDataLoadEventListener.Quiet);
    }
}