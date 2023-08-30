// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Worklog
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Worklog
{
    public string self { get; set; }

    public Author author { get; set; }

    public Author updateAuthor { get; set; }

    public string comment { get; set; }

    public string created { get; set; }

    public string updated { get; set; }

    public string started { get; set; }

    public string timeSpent { get; set; }

    public int timeSpentSeconds { get; set; }

    public string id { get; set; }
}