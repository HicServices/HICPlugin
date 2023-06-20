// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.CreateSupportIssue
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class CreateSupportIssue : CreateIssue
{
    public CreateSupportIssue(
        string projectKey,
        string summary,
        string description,
        string issueTypeIdentifier,
        string customer,
        string email)
        : base(projectKey, summary, issueTypeIdentifier)
    {
        fields.Add(nameof (description), description);
        fields.Add("customfield_12201", customer);
        fields.Add("customfield_12208", email);
    }
}