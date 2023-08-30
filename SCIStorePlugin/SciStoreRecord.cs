using System;
using Rdmp.Core.ReusableLibraryCode;
using Equ;
using Rdmp.Core.ReusableLibraryCode.Annotations;

namespace SCIStorePlugin;

public sealed class SciStoreRecord : PropertywiseEquatable<SciStoreRecord>, IEquatable<SciStoreRecord>
{
    public string CHI;

    public string LabNumber
    {
        get => _labNumber;
        set => _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string TestReportID
    {
        get => _testReportId;
        set => _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string ReportType;
    public string patientid;
    public string testid;
    public string name;
    private string _labNumber;
    private string _testReportId;

    [MemberwiseEqualityIgnore]
    public string Dept { get; set; }

    public static bool operator ==([CanBeNull] SciStoreRecord left, [CanBeNull] SciStoreRecord right)
    {
        return Equals(left, right);
    }

    public static bool operator !=([CanBeNull] SciStoreRecord left, [CanBeNull] SciStoreRecord right)
    {
        return !Equals(left, right);
    }

    public override bool Equals(object obj)
    {
        return obj is SciStoreRecord r && Equals(r);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LabNumber, TestReportID);
    }
}