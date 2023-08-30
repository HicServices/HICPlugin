// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues.Assignee
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Misc;
using Newtonsoft.Json;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues;

public class Assignee : BaseEntity
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonProperty("avatarUrls")]
    public AvatarUrls AvatarUrls { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }
}