using System;
using System.Data;
using System.IO;
using System.Linq;
using HICPluginInteractive.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataExport.DataExtraction.UserPicks;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Unit;

public class CHIColumnFinderTests : TestsRequiringAnExtractionConfiguration
{
    private readonly CHIColumnFinder _chiFinder = new();
    private readonly ThrowImmediatelyDataLoadEventListener _listener = ThrowImmediatelyDataLoadEventListener.QuietPicky;

    [Test]
    [TestCase("1111111111", true)] //valid CHI
    [TestCase(" 1111111111 ", true)] //valid CHI with whitespace either side

    [TestCase(" 09090111111111176766 ", false)] //valid CHI but as part of a longer sequence of numbers

    [TestCase("I've got a lovely bunch of 1111111111 coconuts", true)] //valid CHI in amongst text

    [TestCase("Not so CHI 1111111110", false)] //invalid CHI but which is 10 digits
    [TestCase("This is my CHI:1111111113, I repeat, this is my CHI:1111111111!", true)] //an invalid CHI but which is 10 digits, then a valid 10 digit CHI with special characters and text around it

    [TestCase("1111111111111", false)] //greater than 10 digits should fail (despite containing a valid chi)
    [TestCase("This is 1111111111111 in some text", false)] //same should also fail in amongst text

    [TestCase("111111110", true)] //9 digit CHI (missing leading 0)
    [TestCase("without the initial 0! is 101010109 valid?", true)] //9 digit CHI in amongst text

    [TestCase("1111111111: b j   b hfjb sbdj2009920090", true)] //valid 10 digit CHI at the start and end of the string
    [TestCase("111111110b j   b hfjb sbdj 101010109", true)] //valid 9 digit CHI at the start and end of the string
    [TestCase("hello1111111111   1111111111theend", true)] //valid 10 digit CHIs with text/whitespace directly before and after
    [TestCase("hello111111110   111111110theend", true)] //valid 9 digit CHIs with text/whitespace directly before and after

    [TestCase("111111 1111", true)] //valid 10 digit CHI with whitespace between dob and remaining digits
    [TestCase("here's some text then!111111 1111 full one 111111110", true)] //valid 10 digit CHI with whitespace between dob and remaining digits surrounded by text and another 10 digit CHI
    [TestCase("10101 0109", true)] //valid 9 digit CHI with whitespce between dob and remaining digits
    [TestCase("111111r1111", false)] //valid 10 digit CHI with char between dob and remaining digits

    [TestCase("1111111111 101010109", true)] //valid 10 digit and valid 9 digit with whitespace between
    [TestCase("1111111115 1111111111 101010108 111111110", true)] //invalid 10 digit, valid 10 digit, invalid 9 digit, valid 9 digit, all separated by whitespace
    public void TestDataWithCHIs(string toCheck, bool expectedToBeChi)
    {
        using var toProcess = new DataTable();
        toProcess.Columns.Add("Height");
        toProcess.Rows.Add(new object[] { 195 });
        _chiFinder.PreInitialize(_request, _listener);
        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));

        toProcess.Columns.Add("NothingToSeeHere");
        toProcess.Rows.Add(new object[] { 145, toCheck });
        if (expectedToBeChi)
        {
            Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
            var lines = File.ReadAllLines(_request.GetExtractionDirectory().Parent.Parent.FullName + $"/FoundCHIs/{_request.GetExtractionDirectory().Parent.Name}__potential_CHI_Locations.csv");
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[1].Contains($",{toCheck}"), Is.True);
            File.Delete(_request.GetExtractionDirectory().Parent.Parent.FullName + $"/FoundCHIs/{_request.GetExtractionDirectory().Parent.Name}__potential_CHI_Locations.csv");
        }
        else
            Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));

    }

    [Test]
    [TestCaseSource("CHIS")]
    public void TestForBadString(string toCheck)
    {
        using var toProcess = new DataTable();
        toProcess.Columns.Add("Height");
        toProcess.Rows.Add(new object[] { 195 });
        _chiFinder.PreInitialize(_request, _listener);
        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));

        toProcess.Columns.Add("NothingToSeeHere");
        toProcess.Rows.Add(new object[] { 145, toCheck });
        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
    }

    static object[] CHIS = {
        "e4401697-4561-494f-9b37-e1753686558b",
        "c93c9758-79b2-4d08-95ea-f1e210774568",
        "43b50060-2279-49c6-9ef2-7d404864195e",
        "930d4608-009c-4d73-9292-a201825501a2",
        "cb17401d-8c92-4483-854f-ecb704026279",
        "1896911d-7148-4bc2-8dc2-ddb371240550"
    };

    [Test]
    public void TestFile()
    {
        var memRepo = new MemoryCatalogueRepository();
        var dataRepo = new MemoryDataExportRepository();
        var ds = new ExtractableDataSet(dataRepo, new Catalogue(dataRepo, "cat"));
        var project = new Project(dataRepo, "test")
        {
            ExtractionDirectory = TestContext.CurrentContext.WorkDirectory
        };
        var ec = new ExtractionConfiguration(dataRepo, project, "testConfig");
        ec.AddDatasetToConfiguration(ds);
        foreach (var ecSelectedDataSet in ec.SelectedDataSets)
        {
            ecSelectedDataSet.SaveToDatabase();
        }
        var cf = new CHIColumnFinder();
        var bundle = new ExtractableDatasetBundle(ds)
        {
        };
        var cmd = new ExtractDatasetCommand(ec, bundle);
        cf.PreInitialize(cmd, ThrowImmediatelyDataLoadEventListener.NoisyPicky);
        using var toProcess = new DataTable();
        toProcess.Columns.Add("CHI");
        toProcess.Rows.Add(new object[] { 1111111111 });
        Assert.DoesNotThrow(() => cf.ProcessPipelineData(toProcess, _listener, null));
        var result = Directory.GetFiles(TestContext.CurrentContext.WorkDirectory, "*.csv", SearchOption.AllDirectories)
            .First(static name => name.EndsWith("_Potential_CHI_Locations.csv", StringComparison.Ordinal));
        Assert.That(File.ReadLines(result).ToList().Contains("CHI,1111111111,1111111111"), Is.EqualTo(true));
    }
}