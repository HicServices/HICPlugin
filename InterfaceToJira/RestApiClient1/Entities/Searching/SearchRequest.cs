// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Searching.SearchRequest
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Collections.Generic;
using Newtonsoft.Json;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Searching;

public class SearchRequest
{
    [JsonProperty("jql")]
    public string JQL { get; set; }

    [JsonProperty("startAt")]
    public int StartAt { get; set; }

    [JsonProperty("maxResults")]
    public int MaxResults { get; set; }

    [JsonProperty("fields")]
    public List<string> Fields { get; set; }

    public SearchRequest() => Fields = new List<string>();
}