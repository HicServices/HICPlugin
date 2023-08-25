using System;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;
using HICPluginTests;

namespace SCIStorePluginTests.Unit;

public class CodeValidationTests
{
    [OneTimeSetUp]
    public void BeforeAnyTests()
    {
        //Validator.LocatorForXMLDeserialization = new Mock<IRDMPPlatformRepositoryServiceLocator>().Object;
    }

    [Test]
    public void Test_TestResultWithOneUndefinedCodeBlockWhichIsALocalCode()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"NOT_A_READ_CODE"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            }
        };


        var readCodeConstraint = new MockReferentialIntegrityConstraint();
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.AreEqual("NOT_A_READ_CODE", testDetails.LocalCode.Value);
        Assert.IsNull(testDetails.ReadCode);
    }

    [Test]
    public void Test_TestResultWithOneUndefinedCodeBlockWhichIsAReadCode()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {".0766"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            }
        };


        var readCodeConstraint = new MockReferentialIntegrityConstraint();
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.IsNotNull(testDetails.ReadCode, "The value has not been picked up as a read code");
        Assert.AreEqual(".0766", testDetails.ReadCode.Value);
        Assert.IsNull(testDetails.LocalCode);
    }

    [Test]
    public void Test_TestResultWithTwoUndefinedCodeBlocksWhereOneIsReadAndTheOtherLocal()
    {
        var testType = new TEST_TYPE
        {
            TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"4Q24."}
                        },
                        ClinicalCodeDescription = "C-terminal glucagon level"
                    }
                },
                new CLINICAL_CIRCUMSTANCE_TYPE
                {
                    Item = new CLINICAL_INFORMATION_TYPE
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"GGOC"}
                        },
                        ClinicalCodeDescription = "C-terminal GLUCAGON"
                    }
                }
            }
        };

        var readCodeConstraint = new MockReferentialIntegrityConstraint(static x=>x.Equals("GGOC",StringComparison.Ordinal)?"Not a read code":null);
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.IsNotNull(testDetails.ReadCode);
        Assert.IsNotNull(testDetails.LocalCode);

        Assert.AreEqual("4Q24.", testDetails.ReadCode.Value);
        Assert.AreEqual("GGOC", testDetails.LocalCode.Value);
    }
}