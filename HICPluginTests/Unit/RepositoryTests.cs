using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using HICPluginTests;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Secondary;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;

namespace SCIStorePluginTests.Unit;

class RepositoryTests
{
    [Test]
    public void DeserialisationOfXMLInterferingWithFloats()
    {
        var memoryRepository = new Moq.Mock<IRDMPPlatformRepositoryServiceLocator>().Object;
        Validator.LocatorForXMLDeserialization = memoryRepository;

        var deserializer = new CombinedReportXmlDeserializer();

        var data = deserializer.DeserializeFromXmlString(TestReports.report_with_float_values);

        var readCodeConstraint = new Moq.Mock<ReferentialIntegrityConstraint>();
        var codeValidator = new Func<object, object[], string[], ValidationFailure>((code, cols, colNames) =>
        {
            var codeString = (string) code;
            if (codeString == "TTTT.")
                return null;

            return new ValidationFailure("Not a read code", readCodeConstraint.Object);
        });

        readCodeConstraint.Setup(c => c.Validate(It.IsAny<object>(), It.IsAny<object[]>(), It.IsAny<string[]>()))
            .Returns(codeValidator);

        var reportFactory = new SciStoreReportFactory(readCodeConstraint.Object);
        var report = reportFactory.Create(data, new ThrowImmediatelyDataLoadEventListener());

        var bloodSample = report.Samples.First();
        var result = bloodSample.Results.First(r => r.ReadCodeValue.Equals("TTTT."));

        Assert.AreEqual(8.9, result.QuantityValue.Value);
    }

    [Test]
    public void TestDataTableRepository()
    {
        var report = new SciStoreReport
        {
            Header = new SciStoreHeader
            {
                CHI = "1010101010",
                Discipline = "Test"
            },

            Samples = new HashSet<SciStoreSample>
            {
                new SciStoreSample
                {
                    SampleName = "Blood",
                    LabNumber = "123",
                    TestIdentifier = "TESTID",
                    TestReportID = "234",
                    Results = new []
                    {
                        new SciStoreResult
                        {
                            LabNumber = "123",
                            TestIdentifier = "TESTID",
                            TestReportID = "234",
                            ClinicalCircumstanceDescription = "TESTCCD",
                            ReadCodeValue = "T1",
                            LocalClinicalCodeValue = "TEST_LOCAL",
                            QuantityValue = new decimal(15.2)
                        },
                        new SciStoreResult
                        {
                            LabNumber = "123",
                            TestIdentifier = "ANOTHERTESTID",
                            TestReportID = "234",
                            ClinicalCircumstanceDescription = "ANOTHERTESTIDCCD",
                            ReadCodeValue = "AT1",
                            LocalClinicalCodeValue = "ANOTHERTEST_LOCAL"
                        }
                    }
                }
            }
        };

        var dataTableSchemaSource = new TestDataTableSchemaProvider();
        var repo = new SciStoreDataTableRepository(dataTableSchemaSource);

        var reports = new List<SciStoreReport> {report};
        var listener = new TestDataLoadEventListener();
            
        repo.Create(reports, listener);

        Assert.AreEqual(1, repo.HeadersTable.Rows.Count, "HeadersTable doesn't have the correct number of rows");
        Assert.AreEqual(1, repo.SampleDetailsTable.Rows.Count, "SampleDetailsTable doesn't have the correct number of rows");
        Assert.AreEqual(2, repo.ResultsTable.Rows.Count, "ResultsTable doesn't have the correct number of rows");

        Assert.AreEqual("TESTID", repo.ResultsTable.Rows[0]["TestIdentifier"]);
        Assert.AreEqual("ANOTHERTEST_LOCAL", repo.ResultsTable.Rows[1]["LocalClinicalCodeValue"]);

        Assert.IsTrue(repo.ResultsTable.Rows[0]["QuantityValue"].ToString().Length == 4, "The float value has not been correctly inserted into the data table. Original value was 2.1 (float), value in datatable is " + repo.ResultsTable.Rows[0]["QuantityValue"]);
    }

    [Test]
    public void TestInvalidCharacterReplacement()
    {
        const string badXmlString = @"<TestInterpretation>
                  <Interpretation>
                    for 4 days.&#x1B;(s3B XX to&#x1B;(s&#x1B;(s3Bexclude another pathology.&#x1B;(s0B";

        const string expectedXmlString = @"<TestInterpretation>
                  <Interpretation>
                    for 4 days.[b] XX to[unknown|x1B;(s][b]exclude another pathology.[/b]";

        var deserializer = new CombinedReportXmlDeserializer();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(badXmlString));
        var actualString = deserializer.RemoveInvalidCharactersFromStream(stream);
        Assert.AreEqual(expectedXmlString, actualString);
    }
}

internal class TestDataLoadEventListener : IDataLoadEventListener
{
    public void OnNotify(object sender, NotifyEventArgs e)
    {
        Console.WriteLine($"{e.ProgressEventType}: {e.Message}");
    }

    public void OnProgress(object sender, ProgressEventArgs e)
    {
        Console.WriteLine($"{e}: {e.Progress.Value}");
    }
}

internal class TestDataTableSchemaProvider : IDataTableSchemaSource
{
    public void SetSchema(DataTable dataTable)
    {
        switch (dataTable.TableName)
        {
            case "Header":
                dataTable.Columns.Add(new DataColumn("CHI", typeof(string)));
                break;
            case "SampleDetails":
                dataTable.Columns.Add(new DataColumn("SampleName", typeof(string)));
                break;
            case "Results":
                dataTable.Columns.Add(new DataColumn("TestIdentifier", typeof(string)));
                dataTable.Columns.Add(new DataColumn("LocalClinicalCodeValue", typeof(string)));
                dataTable.Columns.Add(new DataColumn("QuantityValue", typeof (float)));
                break;
            default:
                throw new Exception("Unknown table");
        }

    }
}