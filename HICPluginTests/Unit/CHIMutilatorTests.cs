using System.Linq;
using NUnit.Framework;

namespace HICPluginTests.Unit
{
    class CHIMutilatorTests
    {
        [Test]
        public void Test_CHIMutilator_Construction()
        {
            var repo = new MemoryCatalogueRepository();

            var lmd = new LoadMetadata(repo,"My lmd");
            var pt = new ProcessTask(repo, lmd, LoadStage.AdjustRaw);

            pt.CreateArgumentsForClassIfNotExists(typeof (CHIMutilator));

            //property defaults to true
            var addZero = pt.ProcessTaskArguments.Single(a => a.Name.Equals("TryAddingZeroToFront"));
            Assert.AreEqual(true,addZero);
        }
    }
}
