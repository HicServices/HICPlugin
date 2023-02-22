// Decompiled with JetBrains decompiler
// Type: HIC.Demography.CHIJob
// Assembly: HIC.Demography, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 82227946-33C8-4895-ACC9-8D968B5A9DFA
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\hic.demography\1.0.0\lib\net45\HIC.Demography.dll

using System.Reflection;
using System.Text.RegularExpressions;

namespace HIC.Demography;

public class CHIJob
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
    private static Dictionary<PropertyInfo, int> MaxLengthsDictionary = new Dictionary<PropertyInfo, int>();

    public string TargetServerName { get; set; }

    public string TableName { get; set; }

    public string Forename { get; set; }

    public string Surname { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string Sex { get; set; }

    public string AddressLine1 { get; set; }

    public string AddressLine2 { get; set; }

    public string AddressLine3 { get; set; }

    public string AddressLine4 { get; set; }

    public string Postcode { get; set; }

    public string OtherAddressLine1 { get; set; }

    public string OtherAddressLine2 { get; set; }

    public string OtherAddressLine3 { get; set; }

    public string OtherAddressLine4 { get; set; }

    public string OtherPostcode { get; set; }

    static CHIJob()
    {
        List<PropertyInfo> list1 = ((IEnumerable<PropertyInfo>)typeof(CHIJob).GetProperties()).ToList<PropertyInfo>();
        List<FieldInfo> list2 = ((IEnumerable<FieldInfo>)typeof(CHIJob).GetFields()).ToList<FieldInfo>();
        foreach (PropertyInfo propertyInfo in list1)
        {
            PropertyInfo property = propertyInfo;
            FieldInfo fieldInfo = list2.SingleOrDefault<FieldInfo>((Func<FieldInfo, bool>)(f => f.Name.Equals("MaxSize_" + property.Name)));
            if (fieldInfo != (FieldInfo)null)
                CHIJob.MaxLengthsDictionary.Add(property, (int)fieldInfo.GetValue((object)null));
        }
    }

    public void Clean()
    {
        foreach (PropertyInfo key in CHIJob.MaxLengthsDictionary.Keys)
            key.SetValue((object)this, (object)this.CleanString((string)key.GetValue((object)this, (object[])null)), (object[])null);
        if (this.Sex != null && this.Sex.Length > 1)
        {
            this.Sex = this.Sex.ToUpper();
            if (this.Sex[0] == 'M')
                this.Sex = "M";
            else if (this.Sex[0] == 'F')
                this.Sex = "F";
        }
        if (this.Postcode == null || this.OtherPostcode == null || !this.Postcode.Equals(this.OtherPostcode))
            return;
        this.OtherAddressLine1 = (string)null;
        this.OtherAddressLine2 = (string)null;
        this.OtherAddressLine3 = (string)null;
        this.OtherAddressLine4 = (string)null;
        this.OtherPostcode = (string)null;
    }

    private string CleanString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (string)null;
        value = value.Trim();
        value = Regex.Replace(value, "\\s+", " ");
        return value;
    }

    public CHIJobValidationResult Validate()
    {
        if (string.IsNullOrEmpty(this.TargetServerName))
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TargetServerName was not specified");
        if (string.IsNullOrEmpty(this.TableName))
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TableName was not specified");
        if (this.TableName.Count<char>((Func<char, bool>)(c => c == '.')) != 2)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TableName provided (" + this.TableName + ") must contain exactly 2 dots as in [Database]..[Table] or [Bob].[dbo].[Fish]");
        int num1 = this.TableName.Count<char>((Func<char, bool>)(c => c == '['));
        int num2 = this.TableName.Count<char>((Func<char, bool>)(c => c == ']'));
        if (num1 != num2)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TableName provided (" + this.TableName + ") has a missmatch between the number of open square brackets and the number of closing square brackets");
        if (num1 != 2 && num1 == 3)
            return new CHIJobValidationResult(ValidationCategory.RequestingPartyUnacceptable, "TableName provided (" + this.TableName + ") must have either 2 or 3 openning square brackets e.g. [Database]..[Table] or [Bob].[dbo].[Fish]");
        if (string.IsNullOrWhiteSpace(this.Forename) && string.IsNullOrWhiteSpace(this.Surname) && !this.DateOfBirth.HasValue && string.IsNullOrWhiteSpace(this.Postcode))
            return new CHIJobValidationResult(ValidationCategory.InsufficientData, "Must have at least one of the following: Forename,Surname,DateOfBirth or Postcode");
        foreach (KeyValuePair<PropertyInfo, int> maxLengths in CHIJob.MaxLengthsDictionary)
        {
            if (maxLengths.Key.GetValue((object)this, (object[])null) is string str && str.Length > maxLengths.Value)
                return new CHIJobValidationResult(ValidationCategory.InvalidData, "Field " + (object)maxLengths.Key + " value is too long to fit into the database, value is '" + str + "'");
        }
        return new CHIJobValidationResult(ValidationCategory.Success);
    }
}