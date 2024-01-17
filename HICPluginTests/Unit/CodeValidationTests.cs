using System;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;
using HICPluginTests;
using Rdmp.Core.Validation;
using Tests.Common;

namespace SCIStorePluginTests.Unit;

public class CodeValidationTests : DatabaseTests
{
    [OneTimeSetUp]
    public void BeforeAnyTests()
    {
        Validator.LocatorForXMLDeserialization = RepositoryLocator;
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


        var readCodeConstraint = new MockReferentialIntegrityConstraint(x=>x.Equals("NOT_A_READ_CODE",StringComparison.Ordinal)?"This is not a read code":null);
        var testSetFactory = new TestSetFactory(readCodeConstraint);
        var testDetails = testSetFactory.CreateFromTestType(testType, ThrowImmediatelyDataLoadEventListener.Quiet);

        Assert.Equals("NOT_A_READ_CODE", testDetails.LocalCode.Value);
        Assert.That(testDetails.ReadCode, Is.Null);
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

        Assert.That(testDetails.ReadCode, Is.Not.Null);// "The value has not been picked up as a read code");
        Assert.Equals(".0766", testDetails.ReadCode.Value);
        Assert.That(testDetails.LocalCode, Is.Null);
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

        Assert.That(testDetails.ReadCode,Is.Not.Null);
        Assert.That(testDetails.LocalCode,Is.Not.Null);

        Assert.Equals("4Q24.", testDetails.ReadCode.Value);
        Assert.Equals("GGOC", testDetails.LocalCode.Value);
    }
}