using System;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public enum ClinicalCodeScheme
{
    Read,
    Local,
    Undefined
};

public class ClinicalCodeInfo
{
    public string Value { get; set; }
    public string Scheme { get; set; }
    public ClinicalCodeScheme SchemeId { get; set; }
    public string Description { get; set; }

    public ClinicalCodeInfo(CLINICAL_INFORMATION_TYPE clinicalInformation)
    {
        Value = string.Join(", ", clinicalInformation.ClinicalCode.ClinicalCodeValue);
        Scheme = clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeVersion;

        ClinicalCodeScheme schemeId;
        if (!Enum.TryParse(clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeId, out schemeId))
            throw new Exception(
                $"Unrecognised <ClinicalCodeSchemeId>: {clinicalInformation.ClinicalCode.ClinicalCodeScheme.ClinicalCodeSchemeId}");

        SchemeId = schemeId;
        Description = clinicalInformation.ClinicalCodeDescription;
    }
}