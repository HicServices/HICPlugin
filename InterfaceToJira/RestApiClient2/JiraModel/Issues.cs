// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Issues
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Collections.Generic;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Issues
{
    public int startAt { get; set; }

    public int maxResults { get; set; }

    public int total { get; set; }

    public List<Issue> issues { get; set; }
}