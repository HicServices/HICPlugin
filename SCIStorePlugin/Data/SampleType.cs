using System;
using System.Data;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

public class SampleType
{
    public string Code { get; set; }
    public string CommonCode { get; set; }
    public string Description { get; set; }
    public string HealthBoard { get; set; }

    public SampleType(CLINICAL_CIRCUMSTANCE_TYPE type)
    {
        if (type.Item is string name)
        {
            Description = name;
            Code = Description;
        }

        if (type.Item is CLINICAL_INFORMATION_TYPE clinicalInformation)
        {
            Code = clinicalInformation.ClinicalCode.ClinicalCodeValue[0];
            Description = Code;
        }
    }

    public DataRow PopulateDataRow(DataRow newRow)
    {
        foreach (var prop in GetType().GetProperties())
        {
            if (newRow[prop.Name] == null)
                throw new Exception($"Schema of passed row is incorrect, does not contain a column for '{prop.Name}'");

            newRow[prop.Name] = prop.GetValue(this, null);
        }

        return newRow;
    }
}