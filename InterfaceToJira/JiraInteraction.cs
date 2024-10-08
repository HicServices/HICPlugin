// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JiraInteraction
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using HIC.Common.InterfaceToJira.JIRA.Domain;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;
using InterfaceToJira.RestApiClient2;
using InterfaceToJira.RestApiClient2.JiraModel;
using RestSharp;

namespace HIC.Common.InterfaceToJira;

public class JiraInteraction
{
    private readonly string helpdeskProjectKey;
    private readonly string issueTypeKey;

    public JiraInteraction()
    {
        var apiConfiguration = ConfigurationCheck();
        helpdeskProjectKey = apiConfiguration.HelpdeskProjectKey;
        issueTypeKey = apiConfiguration.IssueTypeKeyCustomerRequest;
    }

    public JiraInteraction(string customProjectKey)
    {
        var apiConfiguration = ConfigurationCheck();
        helpdeskProjectKey = customProjectKey;
        issueTypeKey = apiConfiguration.IssueTypeKeyCustomerRequest;
    }

    public JiraInteraction(string customProjectKey, string customIssueTypeKey)
    {
        ConfigurationCheck();
        helpdeskProjectKey = customProjectKey;
        issueTypeKey = customIssueTypeKey;
    }

    private static JiraApiConfiguration ConfigurationCheck() => ConfigurationManager.GetSection("interfaceToJira") as JiraApiConfiguration ?? throw new ConfigurationErrorsException("Web.config interfaceToJira section is not present!");

    public string RaiseTicket(string summary) => RaiseTicket(summary, string.Empty, helpdeskProjectKey, issueTypeKey);

    public string RaiseTicket(string summary, string description) => RaiseTicket(summary, description, helpdeskProjectKey, issueTypeKey);

    public string RaiseTicket(string summary, string description, string projectKey) => RaiseTicket(summary, description, projectKey, issueTypeKey);

    public string RaiseTicket(
        string summary,
        string description,
        string projectKey,
        string issueType)
    {
        var newIssue = new CreateIssue(projectKey, summary, issueType);
        newIssue.AddField(nameof(description), description);
        return RaiseTicket(newIssue);
    }

    public string RaiseSupportTicket(string summary, string description, string customerEmail) => RaiseSupportTicket(summary, description, string.Empty, customerEmail);

    public string RaiseSupportTicket(
        string summary,
        string description,
        string customerName,
        string customerEmail)
    {
        return RaiseTicket(new CreateSupportIssue(helpdeskProjectKey, summary, description, issueTypeKey, customerName, customerEmail));
    }

    public string RaiseTicket(CreateIssue newIssue)
    {
        try
        {
            return new JiraClient(new JiraAccount()).CreateIssue(newIssue).key;
        }
        catch (JiraApiException ex)
        {
            return ex.Message;
        }
    }

    public List<ExistingTicket> GetLinkMasterForProjectId(int projectId)
    {
        try
        {
            var issuesByJql = new JiraClient(new JiraAccount()).GetIssuesByJql(
                $"project = 'LINK' AND issuetype = 'Master Issue' AND 'Project ID' = {projectId}", 0, 1000);
            if (issuesByJql.Equals(null))
                return null;
            var masterForProjectId = new List<ExistingTicket>();
            foreach (var issue in issuesByJql.issues)
            {
                var existingTicket = new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"));
                masterForProjectId.Add(existingTicket);
            }
            return masterForProjectId;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public bool LinkTickets(string key1, string key2)
    {
        //TODO fix this
        //try
        //{
        //    var jiraClient = new JiraClient(new JiraAccount());
        //    var request = new RestRequest("/rest/api/latest/issueLink", Method.Post)
        //    {
        //        RequestFormat = DataFormat.Json
        //    };
        //    request.AddBody(new
        //    {
        //        type = new { name = "Relates" },
        //        inwardIssue = new { key = key1 },
        //        outwardIssue = new { key = key2 }
        //    });
        //    return jiraClient.Execute(request, HttpStatusCode.OK);
        //}
        //catch (JiraApiException)
        //{
        //    return false;
        //}
        return false;
    }

    [Obsolete("This method is old and should not be used", true)]
    public List<ExistingTicket> GetOpenTickets(string customerName, bool asInvolvedPerson)
    {
        try
        {
            var jiraClient = new JiraClient(new JiraAccount());
            var issues = !asInvolvedPerson ? jiraClient.GetIssuesByJql(
                $"project = 'Customer Requests' AND status != 'Closed' AND 'Customer' = '{customerName}'", 0, 1000) : jiraClient.GetIssuesByJql(
                $"project = 'Customer Requests' AND status != 'Closed' AND 'Other people involved' = '{customerName}'", 0, 1000);
            if (issues.Equals(null))
                return null;
            var openTickets = new List<ExistingTicket>();
            foreach (var issue in issues.issues)
            {
                var existingTicket = new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"));
                openTickets.Add(existingTicket);
            }
            return openTickets;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public List<ExistingTicket> GetSupportRequests(string customerEmail)
    {
        var issuesByJql = new JiraClient(new JiraAccount()).GetIssuesByJql(
            $"project = {helpdeskProjectKey} AND (cf[13515] ~ '{customerEmail}' OR cf[12208] = '{customerEmail}')", 0, 1000);
        if (issuesByJql.Equals(null))
            return null;
        var supportRequests = new List<ExistingTicket>();
        foreach (var issue in issuesByJql.issues)
        {
            var existingTicket = new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"));
            supportRequests.Add(existingTicket);
        }
        return supportRequests;
    }

    public List<ExistingTicket> GetClosedTickets(string customerName)
    {
        try
        {
            var jiraClient = new JiraClient(new JiraAccount());
            //"project = 'Customer Requests' AND status = Closed AND ('Customer' = '" + customerName + "' OR 'Other people involved' ~ '" + customerName + "')";
            var issuesByJql = jiraClient.GetIssuesByJql(
                $"project = 'Customer Requests' AND status = Closed AND ('Customer' = '{customerName}' OR 'Other people involved' = '{customerName}')", 0, 1000);
            return issuesByJql.Equals(null) ? null : issuesByJql.issues.Select(issue => new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"))).ToList();
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public bool UpdateTicket(string key, string newComment)
    {
        try
        {
            var jiraClient = new JiraClient(new JiraAccount());
            var newComment1 = new Comment();
            var defaultAuthor = jiraClient.GetDefaultAuthor();
            newComment1.author = defaultAuthor;
            newComment1.updateAuthor = defaultAuthor;
            newComment1.body = newComment;
            jiraClient.AddComment(key, newComment1);
            return true;
        }
        catch (JiraApiException)
        {
            return false;
        }
    }

    public ExistingTicketWithComments GetTicket(string key, string customerEmail)
    {
        try
        {
            var issue = new JiraClient(new JiraAccount()).GetIssue(key);
            return issue.fields.customfield_12208 != null && issue.fields.customfield_12208 == customerEmail ? new ExistingTicketWithComments(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created), issue.fields.comment, issue.fields.attachment) : null;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public ExistingTicketWithComments GetTicket(string key)
    {
        try
        {
            var issue = new JiraClient(new JiraAccount()).GetIssue(key);
            return new ExistingTicketWithComments(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created), issue.fields.comment, issue.fields.attachment);
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public List<ExistingTicket> GetTicketsForProject(string projectKey)
    {
        try
        {
            var issuesByProject = new JiraClient(new JiraAccount()).GetIssuesByProject(projectKey, 0, 1000);
            if (issuesByProject.Equals(null))
                return null;
            var ticketsForProject = new List<ExistingTicket>();
            foreach (var issue in issuesByProject.issues)
            {
                var existingTicket = new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"));
                ticketsForProject.Add(existingTicket);
            }
            return ticketsForProject;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public List<ExistingTicket> GetSupportTicketsForProject(string projectKey)
    {
        var ticketsForProject = new List<ExistingTicket>();
        try
        {
            var supportIssuesByProject = new JiraClient(new JiraAccount()).GetSupportIssuesByProject(projectKey, 0, 1000);
            if (supportIssuesByProject.Equals(null))
                return ticketsForProject;
            foreach (var issue in supportIssuesByProject.issues)
            {
                var existingTicket = new ExistingTicket(issue.key, issue.fields.summary, issue.fields.description, issue.fields.status.name, DateTime.Parse(issue.fields.created ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.updated ?? "1900-01-01T00:00:00.000+0000"), DateTime.Parse(issue.fields.resolutiondate ?? "1900-01-01T00:00:00.000+0000"));
                ticketsForProject.Add(existingTicket);
            }
            return ticketsForProject;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }

    public List<Project> GetAllProjects()
    {
        try
        {
            var allProjects = new JiraClient(new JiraAccount()).GetAllProjects();
            return allProjects.Equals(null) ? null : allProjects;
        }
        catch (JiraApiException)
        {
            return null;
        }
    }


    //TODO add get all projectAssets
    public List<JiraAsset> GetAllProjectAssets()
    {
        try
        {
            var assets = new JiraAPIClient(new JiraAccount()).GetAllProjectAssets();
            return assets;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}