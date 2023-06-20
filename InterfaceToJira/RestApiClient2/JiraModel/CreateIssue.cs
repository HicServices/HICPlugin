// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.CreateIssue
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Collections.Generic;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class CreateIssue
{
    public readonly Dictionary<string, object> fields;

    public CreateIssue(
        string projectKey,
        string summary,
        string issueTypeIdentifier,
        bool createByName = false)
    {
        fields = new Dictionary<string, object>
        {
            {
                "project",
                new{ key = projectKey }
            },
            {
                nameof (summary),
                summary
            }
        };
        if (createByName)
            fields.Add("issuetype", new
            {
                name = issueTypeIdentifier
            });
        else
            fields.Add("issuetype", new
            {
                id = issueTypeIdentifier
            });
    }

    public void AddField(string fieldName, object value) => fields.Add(fieldName, value);
}