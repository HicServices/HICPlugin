// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraClient
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;
using InterfaceToJira.RestApiClient2;
using InterfaceToJira.RestApiClient2.JiraModel;
using RestSharp;
using RestSharp.Authenticators;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2;

public class JiraClient
{
    private readonly RestClient client;

    public JiraClient(JiraAccount account)
    {
        client = new RestClient(new RestClientOptions(account.ServerUrl)
        {
            Authenticator = new HttpBasicAuthenticator(account.User, account.Password)
        });
        var x = new JiraAPIClient(account);
        var y = x.GetAllProjectAssets();//.ForEach(project => { })
        throw new Exception(y.ToString());
    }

    private static string ToCommaSeparatedString(IEnumerable<string> strings) => strings != null ? string.Join(",", strings) : string.Empty;

    public Issue GetIssue(string issueKey, IEnumerable<string> fields = null)
    {
        var commaSeparatedString = ToCommaSeparatedString(fields);
        var issue = RESTHelper.Execute<Issue>(client, new RestRequest
        {
            Resource = $"/rest/api/latest/issue/{issueKey}?fields={commaSeparatedString}",
            Method = Method.Get
        });
        return issue.fields == null ? null : issue;
    }

    public Issues GetIssuesByJql(
        string jql,
        int startAt,
        int maxResults,
        IEnumerable<string> fields = null)
    {
        var request = new RestRequest("/rest/api/latest/search");
        request.AddParameter(Parameter.CreateParameter(nameof(jql), jql, ParameterType.GetOrPost));
        request.AddParameter(Parameter.CreateParameter(nameof(fields), fields, ParameterType.GetOrPost));
        request.AddParameter(Parameter.CreateParameter(nameof(startAt), startAt, ParameterType.GetOrPost));
        request.AddParameter(Parameter.CreateParameter(nameof(maxResults), maxResults, ParameterType.GetOrPost));
        request.Method = Method.Get;
        return RESTHelper.Execute<Issues>(client, request);
    }

    public Issues GetIssuesByProject(
        string projectKey,
        int startAt,
        int maxResults,
        IEnumerable<string> fields = null)
    {
        return GetIssuesByJql($"project={projectKey}", startAt, maxResults, fields);
    }

    public Issues GetSupportIssuesByProject(
        string projectKey,
        int startAt,
        int maxResults,
        IEnumerable<string> fields = null)
    {
        return GetIssuesByJql($"project={projectKey} AND issuetype = 'Support - Generic'", startAt, maxResults, fields);
    }

    public Issues GetIssuesByProjectId(
        int projectId,
        int startAt,
        int maxResults,
        IEnumerable<string> fields = null)
    {
        return GetIssuesByJql($"project={projectId}", startAt, maxResults, fields);
    }

    public List<Priority> GetPriorities() => RESTHelper.Execute<List<Priority>>(client, new RestRequest
    {
        Resource = "/rest/api/latest/priority",
        Method = Method.Get
    });

    public ProjectMeta GetProjectMeta(string projectKey)
    {
        var request = new RestRequest
        {
            Resource = "/rest/api/latest/issue/createmeta"
        };
        request.AddParameter(Parameter.CreateParameter("projectKeys", projectKey, ParameterType.GetOrPost));
        request.Method = Method.Get;
        var issueCreateMeta = RESTHelper.Execute<IssueCreateMeta>(client, request);
        if (issueCreateMeta.projects[0].key != projectKey || issueCreateMeta.projects.Count != 1)
            throw new JiraApiException();
        return issueCreateMeta.projects[0];
    }

    public List<Status> GetStatuses() => RESTHelper.Execute<List<Status>>(client, new RestRequest
    {
        Resource = "/rest/api/latest/status",
        Method = Method.Get
    });

    public BasicIssue CreateIssue(CreateIssue newIssue)
    {
        var request = new RestRequest
        {
            Resource = "/rest/api/latest/issue",
            RequestFormat = DataFormat.Json,
            Method = Method.Post
        };
        request.AddBody(newIssue);
        return RESTHelper.Execute<BasicIssue>(client, request);
    }

    public ApplicationProperty GetApplicationProperty(string propertyKey)
    {
        var request = new RestRequest
        {
            Method = Method.Get,
            Resource = "/rest/api/latest/application-properties",
            RequestFormat = DataFormat.Json
        };
        request.AddParameter(Parameter.CreateParameter("key", propertyKey, ParameterType.GetOrPost));
        return RESTHelper.Execute<ApplicationProperty>(client, request);
    }

    public Attachment GetAttachment(string attachmentId) => RESTHelper.Execute<Attachment>(client, new RestRequest
    {
        Method = Method.Get,
        Resource = $"/rest/api/latest/attachment/{attachmentId}",
        RequestFormat = DataFormat.Json
    });

    public void DeleteAttachment(string attachmentId)
    {
        var restResponse = client.Execute(new RestRequest
        {
            Method = Method.Delete,
            Resource = ("/rest/api/latest/attachment/" + attachmentId)
        });
        if (restResponse.ResponseStatus != ResponseStatus.Completed || restResponse.StatusCode != HttpStatusCode.NoContent)
            throw new JiraApiException($"Failed to delete attachment with id={attachmentId}");
    }

    public BasicIssue AddComment(string issueKey, Comment newComment)
    {
        var request = new RestRequest
        {
            Resource = $"/rest/api/latest/issue/{issueKey}/comment",
            RequestFormat = DataFormat.Json,
            Method = Method.Post
        };
        request.AddBody(newComment);
        return RESTHelper.Execute<BasicIssue>(client, request);
    }

    public Author GetDefaultAuthor()
    {
        var defaultAuthor = new Author();
        if (ConfigurationManager.GetSection("interfaceToJira") is not JiraApiConfiguration section)
            throw new ConfigurationErrorsException("Web.config interfaceToJira section is not present!");
        defaultAuthor.self = $"{section.ApiUrl}user?username={section.User}";
        defaultAuthor.name = section.User;
        defaultAuthor.displayName = section.UserDisplayName;
        defaultAuthor.emailAddress = section.UserEmail;
        return defaultAuthor;
    }

    public BasicIssue UploadAttachment(string issueKey, Attachment newAttachment)
    {
        var request = new RestRequest
        {
            Resource = $"/rest/api/latest/issue/{issueKey}/attachments",
            RequestFormat = DataFormat.Json,
            Method = Method.Post
        };
        request.AddBody(newAttachment);
        return RESTHelper.Execute<BasicIssue>(client, request);
    }

    public List<string> GetProjectNames()
    {
        var list = RESTHelper.Execute<List<Project>>(client, new RestRequest
        {
            Resource = "/rest/api/latest/project",
            Method = Method.Get
        }).Select((Func<Project, string>)(project => project.key)).ToList();
        list.Sort();
        return list;
    }

    public List<Project> GetAllProjects() => RESTHelper.Execute<List<Project>>(client,new RestRequest
    {
        Resource = "/rest/api/latest/project",
        Method = Method.Get
    });

}