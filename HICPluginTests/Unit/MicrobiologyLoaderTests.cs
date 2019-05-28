using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace HICPluginTests.Unit
{
    public class MicrobiologyLoaderTests
    {
        [Test]
        [TestCase("",false)]
        [TestCase("AB102", false)]
        [TestCase("12AB",true)]
        [TestCase("   12AB   ",true)]
        [TestCase("  12 AB  ", false)]
        [TestCase(", 12AB   ", false)]
        [TestCase("  AB12  ", false)]
        [TestCase("  A B 12 ",false)]
        [TestCase("\t12A ", true)]
        [TestCase("\r\n12A\r\n", true)]
        [TestCase("\r\n12\r\n12A\r\n", true)]
        [TestCase("\r\n  12A\r\n", true)]
        public void TestParsingTestCodes(string val, bool isValidResultCode)
        {
            var ms = new MemoryStream();
            ms.Write(Encoding.ASCII.GetBytes(val), 0, Encoding.ASCII.GetBytes(val).Length);
            ms.Position = 0;

            TextReader tr = new StreamReader(ms);

            MicroBiologyFileReader mb = new MicroBiologyFileReader(tr);

            
            var result1 = mb.GetSpecimenNo(tr);

            //result is not null if it is a valid result code
            Assert.IsTrue(isValidResultCode == (result1 != null));
        }

        [Test]
        public void TestProcessingTestFile_Normal()
        {
            var testString =
            @"
 11B111111
1111111111
FRANK
11 Apr 1111
 F
VTK                     1
POLY                    NR
COM1                    NR
$CULT                   SAUR|P|Y|Y
$SENS                   PEN
                        CLX
                        ERY
                        GEN
                        FUS
                        VAN
                        TRI
                        CIP
                        MUP
                        CLN
                        LIN
                        DAP
                        TEI
                        TET
                        TIG
                        NIT
                        CHL
$SENS                   R
                        S
                        S
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        r
                        s
                        s
                        s

11/11/1111
WS
11:11
Site: right
Upgrade to move by wire cyberware
patient is shadowrunner
SMAC
MB
11 Nov 1111
11:11
THOK1H

 22B222222
1111111111
MASON
11 Apr 1111
 F
BCOM                    P
BCAE                    P
NBC                     GPC ( ? STAPH )
ABC                     GPC ( ? STAPH )
SAGX                    MSGXP
VTK                     1
$CULT                   SAUR|G|Y|Y
$CULT                   BB
$CULT                   REFR
$CULT                   NPVPC
$CULT                   ICB
$CULT                   SMRSA
$SENS                   PEN
                        CLX
                        ERY
                        GEN
                        FUS
                        VAN
                        TRI
                        RIF
                        CIP
                        MUP
                        CLN
                        LIN
                        DAP
                        TEI
                        TET
                        TIG
                        NIT
                        CHL
$SENS                   R
                        S
                        S
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
SAB
11/11/1111
BCV
11:11
upgrade of cranium to new cyberware
patient very excited
N11
MB
11 Nov 1111
11:11
NATD1H

 33B333333
1111111111
FrankSeps
11 Nov 1111
 F
VTK                     1
$CULT                   NG

11/11/1111
MSU
11:11
? UTI |
ACADFOR
MB
11 Nov 1111
11:11
BURD1G

 44B444444
1111111111
GUNN
11 May 1111
 F
VTK                     1
$CULT                   NG

11/11/1111
MSU
11:11
dipstick was wet after test.|Commenced on drying |
CASMON
MB
11 Nov 1111
11:11
WALD1G

 55B555555
1111111111
GOODEY
11 May 1111
 F
VTK                     1
$CULT                   NG

11/11/1111
MSU
11:11
pre assessment clinic |
PPAC
MB
11 Nov 1111
11:11
GORA1H
";
            var results = CreateReaderFromString(testString, true);


            Assert.AreEqual(5, results.Count(r => r is MB_Lab));
            Assert.AreEqual(5, results.Where(r => r is MB_Tests).Cast<MB_Tests>().Count(t=>t.TestCode.Equals("VTK")));

            Assert.AreEqual(35, results.Count(r => r is MB_IsolationResult));
            Assert.AreEqual(2, results.Count(r => r is MB_Isolation));


        }

        private IList<IMicrobiologyResultRecord> CreateReaderFromString(string testString, bool throwOnWarnings)
        {
            TextReader tr = new StreamReader(StringToStream(testString));
            MicroBiologyFileReader mb = new MicroBiologyFileReader(tr);
            mb.Warning += delegate(object sender, string message) { if(throwOnWarnings)throw new Exception("Warning was " + message); };
            var results = mb.ProcessFile().ToList();

            foreach (IMicrobiologyResultRecord r in results)
            {
                Console.Write(r.GetType().Name);
                WriteOutObject(r);
                Console.WriteLine();
            }

            return results;
        }

        private void WriteOutObject(IMicrobiologyResultRecord microbiologyResultRecord)
        {
            Console.Write("(");
            foreach (PropertyInfo p in microbiologyResultRecord.GetType().GetProperties())
                Console.Write(p.Name + ":" + p.GetValue(microbiologyResultRecord) + ",");

            Console.Write(")");
        }

        [Test]
        public void TestProcessingTestFile_MissingIsolationsResults()
        {
            var testString =
            @"
 11B111111
1111111111
FRANK
11 Apr 1111
 F
VTK                     1
POLY                    NR
COM1                    NR
$CULT                   SAUR|P|Y|Y
$CULT                   BOBY|Z|N|N

11/11/1111
WS
11:11
Site: right
Upgrade to move by wire cyberware
patient is shadowrunner
SMAC
MB
11 Nov 1111
11:11
THOK1H
";
            var results = CreateReaderFromString(testString, true);
            Assert.AreEqual(6,results.Count);

            var isolations = results.Where(r => r is MB_Isolation).Cast<MB_Isolation>().ToArray();
            Assert.AreEqual(2,isolations.Count());


            Assert.AreEqual(0, results.Count(r => r is MB_IsolationResult));//should be no results because there are no "$SENS             S" etc bits

            Assert.AreEqual("SAUR",isolations[0].organismCode );
            Assert.AreEqual("P", isolations[0].WeightGrowth_cd);
            Assert.AreEqual("Y",isolations[0].IntCode1);
            Assert.AreEqual("Y",isolations[0].IntCode2);

            Assert.AreEqual("BOBY",isolations[1].organismCode);
            Assert.AreEqual("Z",isolations[1].WeightGrowth_cd);
            Assert.AreEqual("N",isolations[1].IntCode1);
            Assert.AreEqual("N",isolations[1].IntCode2);



        }

        [Test]
        public void TestProcessingFile_CommentsAppearAfterResultsButBeforeDate()
        {
            var testString = @"11B111111

FrankyFrank
11 Nov 1111
 F
BCOM                    NG1
BCAE                    NG1
SENT IN WITHOUT FORM OR ICE REQUEST. DETAILS ON A
HISTORY/CONTINUATION SHEET NP 11/11/11
11/11/1111
BC
11:11
LIKELY APPENDICITIS|PERSISTENT FEVER & SIRS|ON AMOX, GENT, METRO|
N1
MB
11 Nov 1111
11:11
NG
";
            var results = CreateReaderFromString(testString, true);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("LIKELY APPENDICITIS|PERSISTENT FEVER & SIRS|ON AMOX- GENT- METRO|###SENT IN WITHOUT FORM OR ICE REQUEST. DETAILS ON A HISTORY/CONTINUATION SHEET NP 11/11/11", ((MB_Lab)results.Single(r => r is MB_Lab)).Comments);

        }

        [Test]
        public void TestProcessingFile_CommentsAfterIsolations()
        {
            var testString = @"
 11B111111
1111111111
FrankyTheTest
11 May 1111
 M
VTK                     1
POLY                    MP
COM1                    FCPCR
$CULT                   STDYS|L|Y|Y
$CULT                   FIN
$CULT                   NAI
$SENS                   PEN
                        ERY
                        TET
                        LEV
$SENS                   S
                        R
                        R
                        s
 NB THE Sdsfkahjsdfjhasdlfjh HE GROUP G
 adsfasdfasdf
 SEE LAB NUMBER 111111 FOR asdfadsf asdfasdfasdf_______
11/11/1111
SYF
11:11
SITE:  asdfasdf FLUID ASDF KNEE
PAIN asdfasdf asdfsadf KNEE asdf
asdfsdfa BOTH sadfasdf ? asdfasdfasdf asdf
asdf asdf asdf asdfadsf asdf asdf
N11
MB
11 Jul 1111
11:11
LOVG1H
";
            var results = CreateReaderFromString(testString, true);
            Assert.AreEqual(11, results.Count);
        }

        [Test]
        public void TestProcessingFile_RandomCrudAtEndOfFile()
        {
            var testString = @"
 11B111111
1111111111
FrankyTheTest
11 May 1111
 M
VTK                     1
POLY                    MP
COM1                    FCPCR
$CULT                   STDYS|L|Y|Y
$CULT                   FIN
$CULT                   NAI
$SENS                   PEN
                        ERY
                        TET
                        LEV
$SENS                   S
                        R
                        R
                        s
 NB THE Sdsfkahjsdfjhasdlfjh HE GROUP G
 adsfasdfasdf
 SEE LAB NUMBER 111111 FOR asdfadsf asdfasdfasdf_______
11/11/1111
SYF
11:11
SITE:  asdfasdf FLUID ASDF KNEE
PAIN asdfasdf asdfsadf KNEE asdf
asdfsdfa BOTH sadfasdf ? asdfasdfasdf asdf
asdf asdf asdf asdfadsf asdf asdf
N11
MB
11 Jul 1111
11:11
LOVG1H

19160 records listed
The following record ids do not exist:
42630*17060*54

";
            var results = CreateReaderFromString(testString, true);
            Assert.AreEqual(11, results.Count);
            Assert.AreEqual("11:11",((MB_Lab)results.Single(r=>r is MB_Lab)).RcvTime);
            Assert.AreEqual("11:11", ((MB_Lab)results.Single(r => r is MB_Lab)).SampleTime);
        }

        [Test]
        public void TestProcessingFile_Overflow()
        {
            var testString = @"
 11B111111
1111111111
MADGE
11 Jul 1111
 F
TVT                     NTV
CCT                     NCLS
$CULT                   NCA
$SENS                   AUG
                        CFM
                        GEN
                        TAZ
                        CIP
                        TRI
                        AMX
                        TEM
                        CFX
                        CFR
                        CFZ
                        CFI
                        ATM
                        ERT
                        MER
                        AMK
                        TOB
                        TIG

11/11/1111
VUL
11:11
vulvodynia
HILL
MB
11 Jan 1111
11:11
DYMT1G
";
            var results = CreateReaderFromString(testString, false);
            Assert.AreEqual(4,results.Count);
        }

        [Test]
        public void TestProcessingFile_MultipleCultures()
        {
            //3 cultures each with  27 results  (+3 isolation headers)= 84 + 3 test results + 2 no isolations +1 lab

            var testString = @"
 11B111111
1111111111
HOLMES
11 Jul 1111
 M
VTK                     1
POLY                    NR
COM1                    NR
$CULT                   SAUR|P|Y|Y
$CULT                   CALB|P|Y|Y
$CULT                   HINF|P|Y|Y
$CULT                   FC
$CULT                   FPR
$SENS                   PEN
                        CLX
                        ERY
                        GEN
                        FUS
                        VAN
                        TRI
                        RIF
                        CIP
                        MUP
                        CLN
                        LIN
                        DAP
                        TEI
                        TET
                        NIT
                        CHL
                        1FC
                        FLU
                        VOR
                        ATB
                        CFN
                        AMX
                        CFR
                        LEV
                        AUG
                        BLT
$SENS                   R
                        S
                        S
                        s
                        s
                        s
                        S
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        s
                        i
                        s
                        -
                        -
                        -
                        -
                        -
                        R
                        -
                        -
                        S
                        -
$SENS                   -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        s
                        S
                        s
                        s
                        s
                        -
                        -
                        -
                        -
                        -
$SENS                   -
                        -
                        -
                        -
                        -
                        -
                        S
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        S
                        -
                        -
                        -
                        -
                        -
                        -
                        -
                        S
                        s
                        s
                        S
                        n

11/11/1111
ETA
11:11
bob
N11
MB
11 Sep 1111
11:11
JOSJ1H
";
            var results = CreateReaderFromString(testString, false);
            Assert.AreEqual(90, results.Count);

        }

        private Stream StringToStream(string testString)
        {
            var ms = new MemoryStream();
            ms.Write(Encoding.ASCII.GetBytes(testString), 0, Encoding.ASCII.GetBytes(testString).Length);
            ms.Position = 0;

            return ms;
        }

        [Test]
        public void FreakyShortRecordAtEndOfFile()
        {
            var testString = @"
 
11B111111Q
1111111111
ANDERSON
11 Apr 
";

            var ex = Assert.Throws<Exception>(()=>CreateReaderFromString(testString, true));
            StringAssert.Contains("Warning was End of file reached halfway through an MB_Lab record population",ex.Message);


        }


        [Test]
        public void MissingIsolations()
        {
            string testString = @"
11MP111111
1111111111
PIRIE
11 Apr 1111
 F
BLAN
$CULT                   SAUR|P|Y|Y
$CULT                   NEW
$SENS                   P
                        MET
                        E
                        FD
                        CN
                        VA
                        W
                        RD
                        CIP
                        MUP
                        LZD
                        DO
$SENS                   R
                        R
                        S
                        s
                        s
                        S
                        S
                        S
                        r
                        s
                        s
                        S
MRSA
11/11/1111
MRS
11:11
NS|MRSA|
P1
MP
11 Feb 1111
11:11
SHEA1H";
            var results = CreateReaderFromString(testString, true);

            Assert.AreEqual(12,results.Count(r => r is MB_IsolationResult));

            MB_Tests t = ((MB_Tests) results.FirstOrDefault(r => r is MB_Tests));

            Assert.NotNull(t);
            Assert.AreEqual("BLAN", t.TestCode);

        }
    }

    

}
