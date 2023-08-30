// Decompiled with JetBrains decompiler
// Type: HIC.Demography.CHIJob
// Assembly: HIC.Demography, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 82227946-33C8-4895-ACC9-8D968B5A9DFA
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\hic.demography\1.0.0\lib\net45\HIC.Demography.dll

using System.Reflection;
using System.Text.RegularExpressions;

namespace HIC.Demography;

public partial class CHIJob
{
    public const string PersonIDColumnName = "PersonID";
    public const int MaxSize_TargetServerName = 200;
    public const int MaxSize_TableName = 200;
    public const int MaxSize_Forename = 50;
    public const int MaxSize_Surname = 50;
    public const int MaxSize_Sex = 1;
    public const int MaxSize_AddressLine1 = 500;
    public const int MaxSize_AddressLine2 = 100;
    public const int MaxSize_AddressLine3 = 100;
    public const int MaxSize_AddressLine4 = 100;
    public const int MaxSize_Postcode = 10;
    public const int MaxSize_OtherAddressLine1 = 500;
    public const int MaxSize_OtherAddressLine2 = 100;
    public const int MaxSize_OtherAddressLine3 = 100;
    public const int MaxSize_OtherAddressLine4 = 100;
    public const int MaxSize_OtherPostcode = 10;
    private static readonly Dictionary<PropertyInfo, int> MaxLengthsDictionary = new();

    public string? TargetServerName { get; set; }

    public string? TableName { get; set; }

    public string? Forename { get; set; }

    public string? Surname { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Sex { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? AddressLine4 { get; set; }

    public string? Postcode { get; set; }

    public string? OtherAddressLine1 { get; set; }

    public string? OtherAddressLine2 { get; set; }

    public string? OtherAddressLine3 { get; set; }

    public string? OtherAddressLine4 { get; set; }

    public string? OtherPostcode { get; set; }

    static CHIJob()
    {
        foreach (var propertyInfo in typeof(CHIJob).GetProperties())
        {
            var fieldInfo = typeof(CHIJob).GetProperty($"MaxSize_{propertyInfo.Name}");
            if (fieldInfo != null)
                MaxLengthsDictionary.Add(propertyInfo, (int)fieldInfo.GetValue(null));
        }
    }

    public void Clean()
    {
        foreach (var key in MaxLengthsDictionary.Keys)
            key.SetValue(this, CleanString((string)key.GetValue(this, null)), null);
        if (Sex is { Length: > 1 })
        {
            Sex = char.ToUpperInvariant(Sex[0]) switch
            {
                'M' => "M",
                'F' => "F",
                _ => Sex.ToUpper()
            };
        }
        if (Postcode == null || OtherPostcode == null || !Postcode.Equals(OtherPostcode))
            return;
        OtherAddressLine1 = null;
        OtherAddressLine2 = null;
        OtherAddressLine3 = null;
        OtherAddressLine4 = null;
        OtherPostcode = null;
    }

    private static string? CleanString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();
        value = SpacesRegex().Replace(value, " ");
        return value;
    }

    public CHIJobValidationResult Validate()
    {
        if (string.IsNullOrEmpty(TargetServerName))
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TargetServerName was not specified");
        if (string.IsNullOrEmpty(TableName))
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TableName was not specified");
        if (TableName.Count(c => c == '.') != 2)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable,
                $"TableName provided ({TableName}) must contain exactly 2 dots as in [Database]..[Table] or [Bob].[dbo].[Fish]");
        var num1 = TableName.Count(c => c == '[');
        var num2 = TableName.Count(c => c == ']');
        if (num1 != num2)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable,
                $"TableName provided ({TableName}) has a missmatch between the number of open square brackets and the number of closing square brackets");
        if (num1 != 2 && num1 == 3)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable,
                $"TableName provided ({TableName}) must have either 2 or 3 openning square brackets e.g. [Database]..[Table] or [Bob].[dbo].[Fish]");
        if (string.IsNullOrWhiteSpace(Forename) && string.IsNullOrWhiteSpace(Surname) && !DateOfBirth.HasValue && string.IsNullOrWhiteSpace(Postcode))
            return new CHIJobValidationResult(ValidationCategory.InsufficientData, "Must have at least one of the following: Forename,Surname,DateOfBirth or Postcode");
        foreach (KeyValuePair<PropertyInfo, int> maxLengths in MaxLengthsDictionary)
        {
            if (maxLengths.Key.GetValue(this, null) is string str && str.Length > maxLengths.Value)
                return new CHIJobValidationResult(ValidationCategory.InvalidData,
                    $"Field {maxLengths.Key} value is too long to fit into the database, value is '{str}'");
        }
        return new CHIJobValidationResult(ValidationCategory.Success);
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex SpacesRegex();
}