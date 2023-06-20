// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues.IssueKey
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues;

public class IssueKey
{
    public string ProjectKey { get; set; }

    public int IssueId { get; set; }

    public IssueKey()
    {
    }

    public IssueKey(string projectKey, int issueId)
    {
        ProjectKey = projectKey;
        IssueId = issueId;
    }

    public static IssueKey Parse(string issueKeyString)
    {
        var strArray = issueKeyString != null ? issueKeyString.Split('-') : throw new ArgumentNullException("IssueKeyString is null!");
        if (strArray.Length != 2)
            throw new ArgumentException("The string entered is not a JIRA key!");
        var result = 0;
        return int.TryParse(strArray[1], out result) ? new IssueKey(strArray[0], result) : throw new ArgumentException("The string entered could not be parsed, issue id is non-integer!");
    }

    public override string ToString() => string.Format("{0}-{1}", ProjectKey, IssueId);
}