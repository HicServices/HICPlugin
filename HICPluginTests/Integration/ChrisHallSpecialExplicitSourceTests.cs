using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;

class ChrisHallSpecialExplicitSourceTests:TestsRequiringAnExtractionConfiguration
{
    [Test]
    public void TestUse()
    {
        var source = new ChrisHallSpecialExplicitSource();

        source.DatabaseToUse = "master";
        source.Collation = "Latin1_General_Bin";
        source.PreInitialize(_request,new ThrowImmediatelyDataLoadEventListener());

        var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
        Assert.NotNull(chunk);
    }
}