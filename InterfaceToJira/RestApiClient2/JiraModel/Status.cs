// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Status
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Status
{
    public static Status UNKNOWN_STATUS = new Status
    {
        id = "UNKNOWN",
        name = "Unknown",
        description = "Unknown status",
        iconUrl = string.Empty,
        self = string.Empty
    };

    public string self { get; set; }

    public string description { get; set; }

    public string iconUrl { get; set; }

    public string name { get; set; }

    public string id { get; set; }
}