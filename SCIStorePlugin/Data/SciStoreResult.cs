using System;
using System.Text.RegularExpressions;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data
{
    public class SciStoreResult : IComparable
    {
        // Key values
        private string _labNumber;
        private string _testReportID;
        public string LabNumber
        {
            get { return _labNumber; }
            set { _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
        }

        public string TestReportID
        {
            get { return _testReportID; }
            set { _testReportID = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
        }

        public string TestIdentifier { get; set; }

        public decimal? QuantityValue { get; set; }
        public string QuantityUnit { get; set; }
        public string ArithmeticComparator { get; set; }
        public string RangeHighValue { get; set; }
        public string RangeLowValue { get; set; }
        public string RangeUnit { get; set; }
        public string Interpretation { get; set; }
        public int? TestResultOrder { get; set; }

        // Denormalised from TestSetDetails
        public string ClinicalCircumstanceDescription { get; set; } // also a key
        public string ReadCodeValue { get; set; }
        public string ReadCodeScheme { get; set; }
        public string ReadCodeSchemeId { get; set; }
        public string ReadCodeDescription { get; set; }
        public string LocalClinicalCodeValue { get; set; }
        public string LocalClinicalCodeScheme { get; set; }
        public string LocalClinicalCodeSchemeId { get; set; }
        public string LocalClinicalCodeDescription { get; set; }

        [NoMappingToDatabase]
        public TestSet TestPerformed { get; set; }

        #region Primary Key logic (Equals/GetHashCode)
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SciStoreResult)obj);
        }
        protected bool Equals(SciStoreResult other)
        {
            return string.Equals(LabNumber, other.LabNumber) && string.Equals(TestReportID, other.TestReportID) && string.Equals(ClinicalCircumstanceDescription, other.ClinicalCircumstanceDescription) && string.Equals(TestIdentifier, other.TestIdentifier);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = LabNumber.GetHashCode();
                hashCode = (hashCode * 396) ^ TestReportID.GetHashCode();
                hashCode = (hashCode * 397) ^ ClinicalCircumstanceDescription.GetHashCode();
                hashCode = (hashCode * 397) ^ TestIdentifier.GetHashCode();
                return hashCode;
            }
        }
        #endregion
        public void PopulateDenormalisedTestSetDetailsFields()
        {
            //todo these are seperate, one is the resolved test set e.g. 'Liver function tests' and one is the child one(s) 'Alanine transaminase' the two fields you need are TestSet_ClinicalCircumstanceDescription and regular (test level) ClinicalCircumstanceDescription
            ClinicalCircumstanceDescription = TestPerformed.ClinicalCircumstanceDescription;

            if (TestPerformed.ReadCode != null)
            {
                var code = TestPerformed.ReadCode;
                ReadCodeValue = code.Value;
                ReadCodeScheme = code.Scheme;
                ReadCodeSchemeId = code.SchemeId.ToString();
                ReadCodeDescription = code.Description;
            }

            if (TestPerformed.LocalCode != null)
            {
                var code = TestPerformed.LocalCode;
                LocalClinicalCodeValue = code.Value;
                LocalClinicalCodeScheme = code.Scheme;
                LocalClinicalCodeSchemeId = code.SchemeId.ToString();
                LocalClinicalCodeDescription = code.Description;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is SciStoreResult)
            {

                var other = (SciStoreResult)obj;

                //sort on primary key first (what test is for)
                int compareOnPk = string.Compare(ClinicalCircumstanceDescription, other.ClinicalCircumstanceDescription, true);

                if (compareOnPk != 0)
                    return compareOnPk;

                //tests are both for the same thing
                int thisOrder = TestResultOrder ?? int.MinValue;
                int otherOrder = other.TestResultOrder ?? int.MinValue;

                //order by order (as we were informed by the scistore xml bitty called   <DisciplineSpecificValues>TestResultOrder:19</DisciplineSpecificValues>)
                return thisOrder - otherOrder;

            }
            throw new Exception("Can only compare " + this.GetType().FullName + " to other instances of itself");
        }


        /// <summary>
        /// Checks all properties that will hit the database to see if every property is the same between the two objects
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsIdenticalTo(SciStoreResult result)
        {
            foreach (var propertyInfo in result.GetType().GetProperties())
            {
                if (Attribute.IsDefined(propertyInfo, typeof(NoMappingToDatabase)))
                    continue; //property is marked with nomapping so wont end up in database so dont care for checking identicalness


                object val1 = propertyInfo.GetValue(result);
                object val2 = propertyInfo.GetValue(this);

                if (val1 == null)
                    if (val2 == null)
                        continue;
                    else
                        return false;
                else
                    if (!val1.Equals(val2))
                        return false;
            }

            return true;
        }
    }
    
    public class SciStoreResultFactory 
    {
        private readonly ReferentialIntegrityConstraint _readCodeConstraint;

        public SciStoreResultFactory(ReferentialIntegrityConstraint readCodeConstraint)
        {
            _readCodeConstraint = readCodeConstraint;
        }

        public SciStoreResult Create(SciStoreSample sample, TEST_RESULT_TYPE test, IDataLoadEventListener listener)
        {
            var testSetFactory = new TestSetFactory(_readCodeConstraint);

            var result = new SciStoreResult
            {
                LabNumber = sample.LabNumber,
                TestReportID = sample.TestReportID,
                TestIdentifier = sample.TestIdentifier,
                TestPerformed = testSetFactory.CreateFromTestType(test.TestPerformed, listener)
            };
            
            result.PopulateDenormalisedTestSetDetailsFields();

            if (test.TestMeasurement != null)
            {
                // We have a TestMeasurement element in the XML
                foreach (var measurement in test.TestMeasurement)
                    HydrateFromTestMeasurement(result, measurement, test);

                if (test.TestInterpretation != null && test.TestInterpretation.Interpretation != null)
                    result.Interpretation = test.TestInterpretation.Interpretation;
            }
            else if (test.TestInterpretation == null)
            {
                // We have no field in the result element that provides us with information about the measurement value
                result.QuantityValue = null;
                result.QuantityUnit = null;
                result.Interpretation = null;
            }
            else if (test.TestInterpretation.Interpretation != null)
            {
                // We don't have a TestMeasurement, but do have a TestInterpretation
                // 13/12/12 JRG Added to fix bug TestMeasurement is NULL but there could be useful information within the interpretation field
                HydrateFromInterpretation(result, test);
            }
            else // we have a TestInterpretation, but no Interpretation child element. Flag this up for investigation of the XML
            {
                throw new Exception(string.Format("--- LabNumber = {0}, TestReportID = {1}", result.LabNumber, result.TestReportID));
            }

            if (result.ClinicalCircumstanceDescription == null)
                throw new Exception("ClinicalCircumstanceDescription is a primary key and should have been set by now in Lab " + result.LabNumber + "/" + result.TestReportID);

            const string expectedTestResultOrderString = "TestResultOrder:";
            if (test.DisciplineSpecificValues != null && !string.IsNullOrWhiteSpace(test.DisciplineSpecificValues))
            {
                var specificValueString = test.DisciplineSpecificValues;
                if (!specificValueString.StartsWith(expectedTestResultOrderString))
                    throw new Exception("DisciplineSpecificValues contains a previously unencountered string (" + specificValueString + "), investigate Lab Number " + result.LabNumber + "/" + result.TestReportID);

                result.TestResultOrder = int.Parse(specificValueString.Substring(expectedTestResultOrderString.Length));
            }

            return result;
        }

        private void HydrateFromInterpretation(SciStoreResult result, TEST_RESULT_TYPE test)
        {
            result.Interpretation = Clean(test.TestInterpretation.Interpretation);
        }

        /// <summary>
        /// Examines the child item of the <TestMeasurement /> node
        /// </summary>
        private void HydrateFromTestMeasurement(SciStoreResult result, QUANTIFIABLE_RESULT_TYPE measurement, TEST_RESULT_TYPE test)
        {
            // Does <ReferenceLimit /> exist inside <TestMeasurement />
            if (measurement.ReferenceLimit != null)
            {
                var range = measurement.ReferenceLimit.Item as RANGE_TYPE;
                if (range != null)
                {
                    result.RangeLowValue = range.RangeLowValue;
                    result.RangeHighValue = range.RangeHighValue;
                    result.RangeUnit = range.RangeUnit;
                }
            }

            // Test whether the child items is a <MeasurementNumeric /> node
            var measurementNumeric = measurement.Item as VALUE_COMPARATOR_TYPE;
            if (measurementNumeric != null)
            {
                result.QuantityValue = measurementNumeric.Result.QuantityValue;
                result.QuantityUnit = measurementNumeric.Result.QuantityUnit;
                result.ArithmeticComparator = measurementNumeric.ArithmeticComparator;
                return;
            }

            // Test whether the child items is a <MeasurementDescription /> node
            var measurementDescription = measurement.Item as string;
            if (measurementDescription != null)
            {
                result.QuantityValue = null;
                result.QuantityUnit = null;
                result.Interpretation = Clean(measurementDescription);
                return;
            }
        }

        private string Clean(string comment)
        {
            // logic taken from sp_Update_biochem.sql
            var clean = comment.Trim()
                .Replace('|', '-')
                .Replace(',', '-')
                .Replace('"', '-');

            // Multiple spaces
            clean = Regex.Replace(clean, @"\s+", " ");

            return clean;
        }

    }
}
