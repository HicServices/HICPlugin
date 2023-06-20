// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Timetracking
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Timetracking
{
    public string originalEstimate { get; set; }

    public string remainingEstimate { get; set; }

    public string timeSpent { get; set; }

    public int? originalEstimateSeconds { get; set; }

    public int? remainingEstimateSeconds { get; set; }

    public int? timeSpentSeconds { get; set; }
}