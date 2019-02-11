using System;
using System.Data;
using HICPluginInteractive.DataFlowComponents;
using NUnit.Framework;
using ReusableLibraryCode.Progress;

namespace HICPluginTests.Unit
{
    public class CHIColumnFinderTests
    {
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
            var chiFinder = new CHIColumnFinder();
            
            var toProcess = new DataTable();
            toProcess.Columns.Add("Height");
            toProcess.Rows.Add(new object[] {195});

            var listener = new ThrowImmediatelyDataLoadEventListener();
            listener.ThrowOnWarning = true;

            Assert.DoesNotThrow(() => chiFinder.ProcessPipelineData(toProcess, listener, null));
            
            toProcess.Columns.Add("NothingToSeeHere");
            toProcess.Rows.Add(new object[] { 145, toCheck });

            if (expectedToBeChi)
                Assert.Throws<Exception>(() => chiFinder.ProcessPipelineData(toProcess, listener, null));
            else
                Assert.DoesNotThrow(() => chiFinder.ProcessPipelineData(toProcess, listener, null));
        }
    }
}
