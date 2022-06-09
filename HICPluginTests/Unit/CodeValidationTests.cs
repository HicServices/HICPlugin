using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Secondary;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Unit
{
    public class CodeValidationTests
    {
        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            Validator.LocatorForXMLDeserialization = MockRepository.Mock<IRDMPPlatformRepositoryServiceLocator>();
        }

        [Test]
        public void Test_TestResultWithOneUndefinedCodeBlockWhichIsALocalCode()
        {
            var testType = new TEST_TYPE();
            testType.TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE()
                {
                    Item = new CLINICAL_INFORMATION_TYPE()
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE()
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE()
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"NOT_A_READ_CODE"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            };


            var readCodeConstraint = MockRepository.Mock<ReferentialIntegrityConstraint>();
            readCodeConstraint.Stub(
                c => c.Validate(Arg<object>.Is.Equal("NOT_A_READ_CODE"), Arg<object[]>.Is.Anything, Arg<string[]>.Is.Anything))
                .Return(new ValidationFailure("This is not a read code", readCodeConstraint));

            var testSetFactory = new TestSetFactory(readCodeConstraint);
            var testDetails = testSetFactory.CreateFromTestType(testType, new ThrowImmediatelyDataLoadEventListener());

            Assert.AreEqual("NOT_A_READ_CODE", testDetails.LocalCode.Value);
            Assert.IsNull(testDetails.ReadCode);
        }

        [Test]
        public void Test_TestResultWithOneUndefinedCodeBlockWhichIsAReadCode()
        {
            var testType = new TEST_TYPE();
            testType.TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE()
                {
                    Item = new CLINICAL_INFORMATION_TYPE()
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE()
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE()
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {".0766"}
                        },
                        ClinicalCodeDescription = "This is a test"
                    }
                }
            };


            var readCodeConstraint = MockRepository.Mock<ReferentialIntegrityConstraint>();
            readCodeConstraint.Stub(
                c => c.Validate(Arg<object>.Is.Equal(".0766"), Arg<object[]>.Is.Anything, Arg<string[]>.Is.Anything))
                .Return(null);

            var testSetFactory = new TestSetFactory(readCodeConstraint);
            var testDetails = testSetFactory.CreateFromTestType(testType, new ThrowImmediatelyDataLoadEventListener());

            Assert.IsNotNull(testDetails.ReadCode, "The value has not been picked up as a read code");
            Assert.AreEqual(".0766", testDetails.ReadCode.Value);
            Assert.IsNull(testDetails.LocalCode);
        }

        [Test]
        public void Test_TestResultWithTwoUndefinedCodeBlocksWhereOneIsReadAndTheOtherLocal()
        {
            var testType = new TEST_TYPE();
            testType.TestName = new[]
            {
                new CLINICAL_CIRCUMSTANCE_TYPE()
                {
                    Item = new CLINICAL_INFORMATION_TYPE()
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE()
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE()
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"4Q24."}
                        },
                        ClinicalCodeDescription = "C-terminal glucagon level"
                    }
                },
                new CLINICAL_CIRCUMSTANCE_TYPE()
                {
                    Item = new CLINICAL_INFORMATION_TYPE()
                    {
                        ClinicalCode = new CLINICAL_CODE_TYPE()
                        {
                            ClinicalCodeScheme = new CLINICAL_CODE_SCHEME_TYPE()
                            {
                                ClinicalCodeSchemeId = "Undefined",
                                ClinicalCodeSchemeVersion = "Undefined"
                            },
                            ClinicalCodeValue = new [] {"GGOC"}
                        },
                        ClinicalCodeDescription = "C-terminal GLUCAGON"
                    }
                }
            };

            var readCodeConstraint = MockRepository.Mock<ReferentialIntegrityConstraint>();

            readCodeConstraint.Stub(
                c => c.Validate(Arg<object>.Is.Equal("4Q24."), Arg<object[]>.Is.Anything, Arg<string[]>.Is.Anything))
                .Return(null);

            readCodeConstraint.Stub(
                c => c.Validate(Arg<object>.Is.Equal("GGOC"), Arg<object[]>.Is.Anything, Arg<string[]>.Is.Anything))
                .Return(new ValidationFailure("Not a read code", readCodeConstraint));

            var testSetFactory = new TestSetFactory(readCodeConstraint);
            var testDetails = testSetFactory.CreateFromTestType(testType, new ThrowImmediatelyDataLoadEventListener());

            Assert.IsNotNull(testDetails.ReadCode);
            Assert.IsNotNull(testDetails.LocalCode);

            Assert.AreEqual("4Q24.", testDetails.ReadCode.Value);
            Assert.AreEqual("GGOC", testDetails.LocalCode.Value);
        }
    }
}