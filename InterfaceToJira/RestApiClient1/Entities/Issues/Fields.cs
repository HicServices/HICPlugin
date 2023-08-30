// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues.Fields
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using Newtonsoft.Json;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues;

public class Fields
{
    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("status")]
    public Status Status { get; set; }

    [JsonProperty("assignee")]
    public Assignee Assignee { get; set; }
}