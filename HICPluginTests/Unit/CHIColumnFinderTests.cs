using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using ReusableLibraryCode.Progress;

namespace HICPluginTests.Unit
{
    public class CHIColumnFinderTests
    {
        [Test]
        [TestCase("1111111111", true)]
        [TestCase(" 1111111111 ", true)]
        [TestCase("I've got a lovely bunch of 1111111111 coconuts", false)]
        public void TestDataWithCHIs(string chi, bool expectedToBeChi)
        {
            var chiFinder = new CHIColumnFinder();

            var toProcess = new DataTable();
            toProcess.Columns.Add("Height");
            toProcess.Rows.Add(new object[] {195});

            Assert.DoesNotThrow(() => chiFinder.ProcessPipelineData(toProcess, new ThrowImmediatelyDataLoadEventListener(), null));

            toProcess.Columns.Add("NothingToSeeHere");
            toProcess.Rows.Add(new object[] { 145, chi });

            if (expectedToBeChi)
                Assert.Throws<Exception>(() => chiFinder.ProcessPipelineData(toProcess, new ThrowImmediatelyDataLoadEventListener(), null));
            else
                Assert.DoesNotThrow(() => chiFinder.ProcessPipelineData(toProcess, new ThrowImmediatelyDataLoadEventListener(), null));
        }
    }
}
