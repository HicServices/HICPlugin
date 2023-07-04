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

    public override bool Equals(object o)
    {
        if (o is null) return false;
        if (ReferenceEquals(this, o)) return true;
        if (o.GetType()!=GetType()) return false;
        if (o is not SciStoreTableRecord other) return false;
        return Equals(other);
    }

    public bool Equals(SciStoreTableRecord other)
    {
        if (other is null) return false;
        return string.Equals(DatabaseName, other.DatabaseName) && string.Equals(HeaderTable, other.HeaderTable) && string.Equals(SamplesTable, other.SamplesTable) && string.Equals(ResultsTable, other.ResultsTable) && string.Equals(TestCodesTable, other.TestCodesTable) && string.Equals(SampleTypesTable, other.SampleTypesTable);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DatabaseName, HeaderTable, SamplesTable, ResultsTable, TestCodesTable,
            SampleTypesTable);
    }
}