using System;

namespace SCIStorePlugin;

public class SciStoreTableRecord : IEquatable<SciStoreTableRecord>
{
    public string DatabaseName; // if different from Discipline
    public string HeaderTable;
    public string SamplesTable;
    public string ResultsTable;
    public string TestCodesTable;
    public string SampleTypesTable;

    public bool Equals(SciStoreTableRecord other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(DatabaseName, other.DatabaseName) && string.Equals(HeaderTable, other.HeaderTable) && string.Equals(SamplesTable, other.SamplesTable) && string.Equals(ResultsTable, other.ResultsTable) && string.Equals(TestCodesTable, other.TestCodesTable) && string.Equals(SampleTypesTable, other.SampleTypesTable);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (DatabaseName != null ? DatabaseName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (HeaderTable != null ? HeaderTable.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (SamplesTable != null ? SamplesTable.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (ResultsTable != null ? ResultsTable.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (TestCodesTable != null ? TestCodesTable.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (SampleTypesTable != null ? SampleTypesTable.GetHashCode() : 0);
            return hashCode;
        }
    }
}