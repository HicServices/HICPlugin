using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.Validation.Constraints.Secondary;
using ReusableLibraryCode.Progress;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public class TestSetFactory
{
    private readonly ReferentialIntegrityConstraint _readCodeConstraint;

    public TestSetFactory(ReferentialIntegrityConstraint readCodeConstraint)
    {
        _readCodeConstraint = readCodeConstraint;
    }

    public TestSet CreateFromTestType(TEST_TYPE testSetDetails,IDataLoadEventListener listener)
    {
        var testSet = new TestSet();

        var clinicalInformationList = new List<ClinicalCodeInfo>();

        foreach (var clinicalCircumstanceType in testSetDetails.TestName)
        {
            var asClinicalInformationType = clinicalCircumstanceType.Item as CLINICAL_INFORMATION_TYPE;
            if (asClinicalInformationType != null)
            {
                clinicalInformationList.Add(new ClinicalCodeInfo(asClinicalInformationType));
                continue;
            }

            var asString = clinicalCircumstanceType.Item as string;
            if (asString != null)
            {
                if (testSet.ClinicalCircumstanceDescription != null)
                    throw new Exception(
                        $"Multiple <ClinicalCircumstanceDescription>. Encountered: {asString}, but have already seen: {testSet.ClinicalCircumstanceDescription}");

                testSet.ClinicalCircumstanceDescription = asString;
                continue;
            }

            throw new Exception("Could not interpret the <TestName> as either a CLINICAL_INFORMATION_TYPE or string");
        }

        var numClinicalInfos = clinicalInformationList.Count;
        switch (numClinicalInfos)
        {
            case 0:
                if (testSet.ClinicalCircumstanceDescription == null)
                    throw new Exception("Have not managed to extract *any* TestName identifiers for this TestSet");
                testSet.LocalCode = null;
                testSet.ReadCode = null;
                break;
            case 1:
                PopulateAppropriateCodeField(testSet, clinicalInformationList[0],listener);
                break;
            case 2:
                if (clinicalInformationList.Count(info => info.SchemeId == ClinicalCodeScheme.Local) > 1)
                    throw new Exception("Multiple local code entries found in this <TestName>");
                if (clinicalInformationList.Count(info => info.SchemeId == ClinicalCodeScheme.Read) > 1)
                    throw new Exception("Multiple read code entries found in this <TestName>");
                PopulateAppropriateCodeField(testSet, clinicalInformationList[0],listener);
                PopulateAppropriateCodeField(testSet, clinicalInformationList[1],listener);
                break;
            default:
                throw new Exception("Unexpected number of <ClinicalInformation> blocks, investigate...");
        }

        if (testSet.ClinicalCircumstanceDescription == null)
            if (testSet.ReadCode != null)
                testSet.ClinicalCircumstanceDescription = testSet.ReadCode.Description;
            else if (testSet.LocalCode != null)
                testSet.ClinicalCircumstanceDescription = testSet.LocalCode.Description;

        // Even if we have a Read or Local code, the Descriptions may still be null
        if (testSet.ClinicalCircumstanceDescription == null)
            throw new Exception("We have no candidate for ClinicalCircumstanceDescription for TestSetDetails.");

        return testSet;
    }

    private void PopulateAppropriateCodeField(TestSet testSet, ClinicalCodeInfo clinicalInformationList,IDataLoadEventListener listener)
    {
        switch (clinicalInformationList.SchemeId)
        {
            case ClinicalCodeScheme.Read:
                testSet.ReadCode = clinicalInformationList;
                break;
            case ClinicalCodeScheme.Local:
                testSet.LocalCode = clinicalInformationList;
                break;
            case ClinicalCodeScheme.Undefined:
                if (ContainsLegitReadCode(clinicalInformationList))
                    if (testSet.ReadCode == null)
                        testSet.ReadCode = clinicalInformationList;
                    else
                    {
                        // We have two read codes for the same TestSet, attempt to settle on one by testing against our set of suspicious read codes
                        DetermineWhichLooksMostLegitAndAssign(testSet, clinicalInformationList, testSet.ReadCode,listener);
                    }
                else if (testSet.LocalCode == null)
                    testSet.LocalCode = clinicalInformationList;
                else
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                        $"Encountered something that was not a readcode ('{clinicalInformationList.Value}') but we had already encountered a nonreadcode({testSet.LocalCode.Value}) - so we are just going to treat it like a readcode anyway"));
                    testSet.ReadCode = clinicalInformationList;
                }
                break;
            default:
                throw new NotSupportedException(clinicalInformationList.SchemeId.ToString());
        }
    }

    #region Multiple read code resolution
    private void DetermineWhichLooksMostLegitAndAssign(TestSet testSet, ClinicalCodeInfo readCode1, ClinicalCodeInfo readCode2,IDataLoadEventListener listener)
    {
        var kvp1 = new KeyValuePair<string, string>(readCode1.Value, readCode1.Description);
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
            $"Two valid read codes found in TestSet: {readCode1.Value}, {readCode2.Value}."));

        if (_highlySuspectSupposedReadCodes.Contains(kvp1))
        {
            var kvp2 = new KeyValuePair<string, string>(readCode2.Value, readCode2.Description);
            if (_highlySuspectSupposedReadCodes.Contains(kvp2))
                throw new Exception(
                    $"We have found two potential read codes for this TestSet, however they both look dodgy: {readCode1.Value}, {readCode2.Value}");

            // readCode2 looks ok, go with that
            testSet.ReadCode = readCode2;
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Have chosen {readCode2.Value} ({readCode2.Description})"));
            if (testSet.LocalCode != null) return;

            // if we haven't already set a local code, use readCode1 in case it actually is a local code rather than a read code
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Local code is null, so using what looks like a dodgy read code but is probably a local code: {readCode1.Value} ({readCode1.Description})"));
            testSet.LocalCode = readCode1;
        }
        else
        {
            // readCode1 looks ok, go with that
            testSet.ReadCode = readCode1;
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Have chosen {readCode1.Value} ({readCode1.Description})"));
            if (testSet.LocalCode != null) return;

            // if we haven't already set a local code, use readCode2 in case it actually is a local code rather than a read code
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Local code is null, so using what looks like a dodgy read code but is probably a local code: {readCode2.Value} ({readCode2.Description})"));
            testSet.LocalCode = readCode2;
        }
    }

    private bool ContainsLegitReadCode(ClinicalCodeInfo info)
    {
        if (_readCodeConstraint == null)
            throw new InvalidOperationException("Cannot check if read code is valid, the constraint object has not been supplied.");

        if (KnownWrongTranscriptions.ContainsKey(info.Value))
            info.Value = KnownWrongTranscriptions[info.Value]; // todo: surprise mutation! need to refactor this/decide on other way to deal with these errors in the data

        return _readCodeConstraint.Validate(info.Value, null, null) == null;
    }

    private readonly Dictionary<string, string> KnownWrongTranscriptions = new Dictionary<string, string>
    {
        {"441D.", "44lD."} // As of 9/1/15, 441D. is not a valid read code
    };

    // This lookup is used if we have detected more than one read code
    // 5/1/15 GM: A specific problem with GP has been fixed, but I'm leaving this here until we have performed a longer-term load of all labs data in case other cases are detected
    private readonly List<KeyValuePair<string, string>> _highlySuspectSupposedReadCodes = new List<KeyValuePair<string, string>>
    {
    };
    #endregion

}