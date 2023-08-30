// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraAccount
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;
using System.Configuration;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2;

[Serializable]
public class JiraAccount
{
    public string ServerUrl;
    public string User;
    public string Password;

    public JiraAccount()
    {
        ServerUrl = ConfigurationManager.GetSection("interfaceToJira") is JiraApiConfiguration section ? section.ServerUrl : throw new ConfigurationErrorsException("Web.config interfaceToJira section is not present!");
        User = section.User;
        Password = section.Password;
    }

    public JiraAccount(JiraApiConfiguration config)
    {
        ServerUrl = config.ServerUrl;
        User = config.User;
        Password = config.Password;
    }
}