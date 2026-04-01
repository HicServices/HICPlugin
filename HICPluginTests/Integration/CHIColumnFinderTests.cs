using HICPlugin.DataFlowComponents;
using HICPluginInteractive.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Data;
using System.IO;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;
class CHIColumnFinderTests : TestsRequiringAnExtractionConfiguration
{
    private readonly CHIColumnFinder _chiFinder = new();
    private readonly ThrowImmediatelyDataLoadEventListener _listener = ThrowImmediatelyDataLoadEventListener.QuietPicky;
    [Test]
    public void IgnoreColumnsAvoidsCHIChecking()
    {
        using var toProcess = new DataTable();
        toProcess.Columns.Add("Height");
        toProcess.Rows.Add(new object[] { 195 });

        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));

        toProcess.Columns.Add("NothingToSeeHere");
        toProcess.Rows.Add(new object[] { 145, "1111111111" });

        Assert.Throws<Exception>(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
        var fileName = Path.GetTempFileName();
        var fileInfo = new FileInfo(fileName);
        fileInfo.Attributes = FileAttributes.Temporary;
        StreamWriter streamWriter = File.AppendText(fileName);
        streamWriter.WriteLine("RDMP_ALL:");
        streamWriter.WriteLine("    - NothingToSeeHere");
        streamWriter.Flush();
        streamWriter.Close();
        _chiFinder.AllowListFile = fileInfo.FullName;

        _chiFinder.PreInitialize(_request, ThrowImmediatelyDataLoadEventListener.Quiet);
        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
    }

    [Test]
    public void CHIMod10Check()
    {
        using var toProcess = new DataTable();
        toProcess.Columns.Add("Height");
        toProcess.Columns.Add("CHI");
        toProcess.Rows.Add(new object[] { 145, "0106851230" });
        DataTable result;
        var exc= Assert.Throws<Exception>(() => result = _chiFinder.ProcessPipelineData(toProcess, _listener, null));
    }
}
