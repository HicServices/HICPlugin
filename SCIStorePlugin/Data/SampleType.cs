using System;
using System.Data;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data
{
    public class SampleType
    {
        public string Code { get; set; }
        public string CommonCode { get; set; }
        public string Description { get; set; }
        public string HealthBoard { get; set; }

        public SampleType(CLINICAL_CIRCUMSTANCE_TYPE type)
        {
            var name = type.Item as string;
            if (name != null)
            {
                Description = name;
                Code = Description;
            }

            var clinicalInformation = type.Item as CLINICAL_INFORMATION_TYPE;
            if (clinicalInformation != null)
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
                    throw new Exception("Schema of passed row is incorrect, does not contain a column for '" + prop.Name + "'");

                newRow[prop.Name] = prop.GetValue(this, null);
            }

            return newRow;
        }
    }
}