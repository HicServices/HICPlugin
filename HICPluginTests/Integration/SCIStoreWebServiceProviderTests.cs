using System.IO;
using System.Xml.Serialization;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Secondary;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using SCIStorePlugin.Data;
using Tests.Common;

namespace SCIStorePluginTests.Integration
{
    [Category("Database")]
    public class SCIStoreWebServiceProviderTests : DatabaseTests
    {
        [Test]
        public void LabWithDifferentClinicalCodeDescriptionsForSameTestCode()
        {
            Validator.LocatorForXMLDeserialization = RepositoryLocator;

            var serializer = new XmlSerializer(typeof(CombinedReportData));
            var lab = serializer.Deserialize(new StringReader(TestReports.report_with_multiple_descriptions)) as CombinedReportData;

            var readCodeConstraint = MockRepository.GenerateStub<ReferentialIntegrityConstraint>();
            readCodeConstraint.Stub(
                c => c.Validate(Arg<object>.Is.Anything, Arg<object[]>.Is.Anything, Arg<string[]>.Is.Anything))
                .Return(null);

            var reportFactory = new SciStoreReportFactory(readCodeConstraint);
            var report = reportFactory.Create(lab, new ThrowImmediatelyDataLoadEventListener());
        }
    }
}
