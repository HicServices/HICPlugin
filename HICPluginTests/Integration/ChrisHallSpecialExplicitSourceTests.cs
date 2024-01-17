using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;

class ChrisHallSpecialExplicitSourceTests:TestsRequiringAnExtractionConfiguration
{
    [Test]
    public void TestUse()
    {

        //DataExtractionSpecialExplicitSource
        var source = new ChrisHallSpecialExplicitSource
        {
            DatabaseToUse = "master",
            Collation = "Latin1_General_Bin"
        };

        source.PreInitialize(_request,ThrowImmediatelyDataLoadEventListener.Quiet);

        var chunk = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
        Assert.That(chunk, Is.Not.Null);
    }
}