// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.Domain.ExistingTicket
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;

namespace HIC.Common.InterfaceToJira.JIRA.Domain;

public class ExistingTicket
{
    public string Key { get; set; }

    public string Summary { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public DateTime? Created { get; set; }

    public DateTime? Updated { get; set; }

    public DateTime? Resolved { get; set; }

    public ExistingTicket(
        string key,
        string summary,
        string description,
        string status,
        DateTime created,
        DateTime updated,
        DateTime resolved)
    {
        Key = key;
        Summary = summary;
        Description = description;
        Status = status;
        if (created.Year > 1900)
            Created = created;
        if (updated.Year > 1900)
            Updated = updated;
        if (resolved.Year <= 1900)
            return;
        Resolved = resolved;
    }
}