// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.Domain.ExistingTicketWithComments
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;
using System.Collections.Generic;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

namespace HIC.Common.InterfaceToJira.JIRA.Domain;

public class ExistingTicketWithComments
{
    public string Key { get; set; }

    public string Summary { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public DateTime Created { get; set; }

    public Comments Comments { get; set; }

    public List<Attachment> Attachments { get; set; }

    public ExistingTicketWithComments(
        string key,
        string summary,
        string description,
        string status,
        DateTime created,
        Comments comments,
        List<Attachment> attachments)
    {
        Key = key;
        Summary = summary;
        Description = description;
        Status = status;
        Created = created;
        Comments = comments;
        Attachments = attachments;
    }
}