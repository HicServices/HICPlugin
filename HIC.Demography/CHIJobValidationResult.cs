// Decompiled with JetBrains decompiler
// Type: HIC.Demography.CHIJobValidationResult
// Assembly: HIC.Demography, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 82227946-33C8-4895-ACC9-8D968B5A9DFA
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\hic.demography\1.0.0\lib\net45\HIC.Demography.dll

namespace HIC.Demography;

public class CHIJobValidationResult
{
    public CHIJobValidationResult(ValidationCategory result) => this.Result = result;

    public CHIJobValidationResult(ValidationCategory result, string reason)
    {
        this.Result = result;
        this.Reason = reason;
    }

    public string Reason { get; set; }

    public ValidationCategory Result { get; set; }
}