// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.JiraApiException
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

[Serializable]
public class JiraApiException : Exception
{
    public JiraApiException()
    {
    }

    public JiraApiException(string message)
        : base(message)
    {
    }

    public JiraApiException(string message, Exception inner)
        : base(message, inner)
    {
    }
}