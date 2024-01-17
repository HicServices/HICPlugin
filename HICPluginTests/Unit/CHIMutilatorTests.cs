using System.Linq;
using HICPlugin.Mutilators;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.DataLoad;
using Tests.Common;

namespace HICPluginTests.Unit;

class CHIMutilatorTests:UnitTests
{
    [Test]
    public void Test_CHIMutilator_Construction()
    {
        var lmd = new LoadMetadata(Repository,"My lmd");
        var pt = new ProcessTask(Repository, lmd, LoadStage.AdjustRaw);

        pt.CreateArgumentsForClassIfNotExists(typeof (CHIMutilator));

        //property defaults to true
        var addZero = pt.ProcessTaskArguments.Single(static a => a.Name.Equals("TryAddingZeroToFront"));
        Assert.Equals(true,addZero.GetValueAsSystemType());
    }
}