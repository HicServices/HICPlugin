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
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(LabNumber, other.LabNumber) && string.Equals(TestReportID, other.TestReportID);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SciStoreRecord) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LabNumber, TestReportID);
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