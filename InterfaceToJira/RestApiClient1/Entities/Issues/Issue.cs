// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues.Issue
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using Newtonsoft.Json;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues;

public class Issue : BaseEntity
{
    private string m_KeyString;

    [JsonProperty("expand")]
    public string Expand { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("key")]
    public string ProxyKey
    {
        get => Key.ToString();
        set => m_KeyString = value;
    }

    [JsonIgnore]
    public IssueKey Key => IssueKey.Parse(m_KeyString);

    [JsonProperty("fields")]
    public Fields Fields { get; set; }
}