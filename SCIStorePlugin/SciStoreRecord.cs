using System;
using Rdmp.Core.ReusableLibraryCode;

namespace SCIStorePlugin;

public class SciStoreRecord : IEquatable<SciStoreRecord>
{
    public string CHI;

    public string LabNumber
    {
        get { return _labNumber; }
        set
        {
            _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value);
        }
    }

    public string TestReportID
    {
        get { return _testReportId; }
        set
        {
            _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value);
        }
    }

    public string ReportType;
    public string patientid;
    public string testid;
    public string name;
    private string _labNumber;
    private string _testReportId;
    public string Dept { get; set; }

    public bool Equals(SciStoreRecord other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(LabNumber, other.LabNumber) && string.Equals(TestReportID, other.TestReportID);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SciStoreRecord) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((LabNumber != null ? LabNumber.GetHashCode() : 0)*397) ^ (TestReportID != null ? TestReportID.GetHashCode() : 0);
        }
    }

    public static bool operator ==(SciStoreRecord left, SciStoreRecord right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SciStoreRecord left, SciStoreRecord right)
    {
        return !Equals(left, right);
    }
}