using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Validation.Constraints.Secondary;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public class SciStoreSample
{
    private string _labNumber;
    private string _testReportId;

    public string LabNumber
    {
        get { return _labNumber; }
        set { _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
    }

    public string TestReportID
    {
        get { return _testReportId; }
        set { _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
    }

    public string SampleName { get; set; }
    public DateTime? DateTimeSampled { get; set; }
    public DateTime? DateTimeReceived { get; set; }
    public string SampleRequesterComment { get; set; }
    public string ServiceProviderComment { get; set; }
    public string TestIdentifier { get; set; }
        
    // Denormalised from TestSetDetails
    public string TestSet_ClinicalCircumstanceDescription { get; set; }
    public string TestSet_ReadCodeValue { get; set; }
    public string TestSet_ReadCodeScheme { get; set; }
    public string TestSet_ReadCodeSchemeId { get; set; }
    public string TestSet_ReadCodeDescription { get; set; }
    public string TestSet_LocalClinicalCodeValue { get; set; }
    public string TestSet_LocalClinicalCodeScheme { get; set; }
    public string TestSet_LocalClinicalCodeSchemeId { get; set; }
    public string TestSet_LocalClinicalCodeDescription { get; set; }
        
    [NoMappingToDatabase]
    public TestSet TestSetDetails { get; set; }
    [NoMappingToDatabase]
    public ICollection<SciStoreResult> Results { get; set; }
        
    public void PopulateDenormalisedTestSetDetailsFields()
    {
        TestSet_ClinicalCircumstanceDescription = TestSetDetails.ClinicalCircumstanceDescription;
            
        if (TestSetDetails.ReadCode != null)
        {
            var code = TestSetDetails.ReadCode;
            TestSet_ReadCodeValue = code.Value;
            TestSet_ReadCodeScheme = code.Scheme;
            TestSet_ReadCodeSchemeId = code.SchemeId.ToString();
            TestSet_ReadCodeDescription = code.Description;
        }

        if (TestSetDetails.LocalCode != null)
        {
            var code = TestSetDetails.LocalCode;
            TestSet_LocalClinicalCodeValue = code.Value;
            TestSet_LocalClinicalCodeScheme = code.Scheme;
            TestSet_LocalClinicalCodeSchemeId = code.SchemeId.ToString();
            TestSet_LocalClinicalCodeDescription = code.Description;
        }
    }
        
    public int ResolveTestResultOrderDuplication()
    {
        int resolutions = 0;

        //todo potentially change this to .AddResult method and make Results private
        if(Results is List<SciStoreResult>)
        {

            List<SciStoreResult> toWorkOn = (List<SciStoreResult>) Results;
            toWorkOn.Sort();
            

            for (int index = Results.Count-2; index >= 0; index--)
            {
                var previous = toWorkOn[index];
                var result = toWorkOn[index + 1];

                //only remove values that are EXACT duplicates (not just on primary key)
                if (previous.IsIdenticalTo(result))
                {
                    resolutions++;
                    toWorkOn.RemoveAt(index);
                }
            }

            Results = new HashSet<SciStoreResult>(toWorkOn);
        }
        else
        {
            throw new Exception(
                $"Results is an {Results.GetType()} excpected it to be a List, possibly you have called this method multiple times or something");
        }

        return resolutions;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SciStoreSample) obj);
    }

    protected bool Equals(SciStoreSample other)
    {
        return string.Equals(LabNumber, other.LabNumber) && string.Equals(TestIdentifier, other.TestIdentifier) && string.Equals(TestReportID, other.TestReportID);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = LabNumber.GetHashCode();
            hashCode = (hashCode * 397) ^ TestIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ TestReportID.GetHashCode();
            return hashCode;
        }
    }
}

public class SciStoreSampleFactory 
{
    private readonly ReferentialIntegrityConstraint _readCodeConstraint;

    public SciStoreSampleFactory(ReferentialIntegrityConstraint readCodeConstraint)
    {
        _readCodeConstraint = readCodeConstraint;
    }

    public SciStoreSample Create(SciStoreHeader header, ResultSuite resultSuite, TEST_SET_RESULT_TYPE testResultSet, IDataLoadEventListener listener)
    {
        var testSetDetailsFactory = new TestSetFactory(_readCodeConstraint);
        var resultFactory = new SciStoreResultFactory(_readCodeConstraint);

        var sample = new SciStoreSample()
        {
            LabNumber = header.LabNumber,
            TestReportID = header.TestReportID,
            Results = new List<SciStoreResult>()
        };

        if (testResultSet.TestSetDetails == null)
            throw new Exception($"Sample in Lab {sample.LabNumber}/{sample.TestReportID} has no TestSetDetails block");

        try
        {
            var testSetDetails = testResultSet.TestSetDetails;

            if (testSetDetails.TestIdentifier == null)
                throw new Exception(
                    $"TestIdentifier is a primary key, cannot be null in Lab {sample.LabNumber}/{sample.TestReportID}");

            sample.TestIdentifier = testSetDetails.TestIdentifier.IdValue;
            sample.TestSetDetails = testSetDetailsFactory.CreateFromTestType(testSetDetails, listener);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error when creating the TestSetDetails object for Lab {sample.LabNumber}/{sample.TestReportID}: {e}");
        }

        sample.PopulateDenormalisedTestSetDetailsFields();
        PopulateSampleDetails(sample, resultSuite.SampleDetails);

        if (resultSuite.TestResultSets == null)
            throw new Exception($"<TestResultSets> is null in Lab {sample.LabNumber}");

        var testSet = testResultSet.TestSetDetails.TestName;
        if (!testSet.Any())
            throw new Exception($"This TestResultSet has no TestSetDetails: Lab {sample.LabNumber}");

        if (testResultSet.TestResults == null)
        {
            // This TestResultSet does not have any results associated with it, e.g. has a TestSetDetails containing 'Serum', but no TestResults
            return sample;
        }

        foreach (var testResult in testResultSet.TestResults)
            sample.Results.Add(resultFactory.Create(sample, testResult, listener));

        if (sample.TestSetDetails.ClinicalCircumstanceDescription == null)
            throw new Exception(
                $"The TestSet's ClinicalCircumstanceDescription is a primary key and should have been set by now in Lab {sample.LabNumber}/{sample.TestReportID}");

        return sample;
    }

    private void PopulateSampleDetails(SciStoreSample sample, SAMPLE_TYPE sampleDetails)
    {
        // todo - add nicer way of specifying which properties we don't pull out but are potentially available through the API

        if (sampleDetails.SampleAmount != null)
            throw new Exception($"SampleAmount present in Lab {sample.LabNumber}. Please investigate.");

        if (sampleDetails.TissueType != null)
            throw new Exception($"TissueType present in Lab {sample.LabNumber}. Please investigate.");

        // todo: check this, SampleName is null in Tayside Immunology report. For now, setting it to 'Undefined' if a null value is encountered.
        if (sampleDetails.SampleName == null)
        {
            sample.SampleName = "Undefined";
            //throw new Exception("No SampleName in Lab " + LabNumber);
        }
        else
        {
            var sampleNames = sampleDetails.SampleName;
            if (sampleNames.Length > 1)
                throw new Exception($"Not expecting multiple sample names (found {sampleNames.Length})");

            var sampleNameItem = sampleNames[0].Item as string;
            if (sampleNameItem == null)
                throw new Exception(
                    $"Could not interpret the sample name as a string in Lab {sample.LabNumber}. Will likely be a 'CLINICAL_INFORMATION_TYPE' but this hasn't been encountered during build, please investigate.");

            sample.SampleName = sampleNameItem;
        }

        if (sampleDetails.SampleRequesterComment != null)
            sample.SampleRequesterComment = string.Join(" ", sampleDetails.SampleRequesterComment);

        if (sampleDetails.ServiceProviderComment != null)
            sample.ServiceProviderComment = string.Join(",", sampleDetails.ServiceProviderComment);

        sample.DateTimeSampled = (sampleDetails.DateTimeSampled == DateTime.MinValue) ? (DateTime?) null : sampleDetails.DateTimeSampled;
        sample.DateTimeReceived = (sampleDetails.DateTimeReceived == DateTime.MinValue ) ? (DateTime?) null : sampleDetails.DateTimeReceived;
    }
}