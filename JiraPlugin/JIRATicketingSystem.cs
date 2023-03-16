using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using HIC.Common.InterfaceToJira;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace JiraPlugin;

public class JIRATicketingSystem : PluginTicketingSystem
{
    public static readonly Regex RegexForTickets = new Regex(@"((?<!([A-Z]{1,10})-?)[A-Z]+-\d+)",RegexOptions.IgnoreCase|RegexOptions.Compiled);

    private const string RegexForUrlsPattern = @"https://.*";
    private static readonly Regex RegexForUrls = new Regex(RegexForUrlsPattern,RegexOptions.Compiled);

    private JiraClient _client;

    //releaseability
    public List<Attachment> JIRAProjectAttachements { get; private set; }
    public string JIRAReleaseTicketStatus { get; private set; }
    private static string[] PermissableReleaseStatusesForJIRAReleaseTickets = new[] { "Released" };


    public JIRATicketingSystem(TicketingSystemConstructorParameters parameters) : base(parameters)
    {
           
    }
        
    public override void Check(ICheckNotifier notifier)
    {
        if (Credentials == null)
            notifier.OnCheckPerformed(new CheckEventArgs("Data Access credentials for JIRA are not set",CheckResult.Fail));
            
        if (string.IsNullOrWhiteSpace(Url))
            notifier.OnCheckPerformed(new CheckEventArgs("You must put in a URL to the JIRA server e.g. https://jira-hic.cmdn.dundee.ac.uk",CheckResult.Fail));
        else
        if (RegexForUrls.IsMatch(Url))
            notifier.OnCheckPerformed(new CheckEventArgs("Url matches RegexForUrls", CheckResult.Success));
        else
            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    $"Url {Url} does not match the regex RegexForUrls: {RegexForUrlsPattern}",
                    CheckResult.Fail));
        try
        {
            SetupIfRequired();
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("SetupIfRequired failed", CheckResult.Fail, e));
        }

        try
        {
            var projects = _client.GetProjectNames();
                
            notifier.OnCheckPerformed(new CheckEventArgs($"Found {projects.Count} projects",
                projects.Count == 0 ? CheckResult.Warning : CheckResult.Success));
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Could not fetch issues", CheckResult.Fail, e));
        }
    }

    public override bool IsValidTicketName(string ticketName)
    {
        //also let user clear tickets :)
        return string.IsNullOrWhiteSpace(ticketName) || RegexForTickets.IsMatch(ticketName);
    }

    public override void NavigateToTicket(string ticketName)
    {
        if (string.IsNullOrWhiteSpace(ticketName))
            return;
        try
        {
            Check(new ThrowImmediatelyCheckNotifier());
        }
        catch (Exception e)
        {
            throw new Exception("JIRATicketingSystem Checks() failed (see inner exception for details)",e);
        }


        Uri navigationUri = null;
        Uri baseUri = null;
        var relativePath = $"/browse/{ticketName}";
        try
        {
            baseUri = new Uri(Url);
            navigationUri = new Uri(baseUri, relativePath);
            Process.Start(navigationUri.AbsoluteUri);
        }
        catch (Exception e)
        {
            if(navigationUri != null)
                throw new Exception($"Failed to navigate to {navigationUri.AbsoluteUri}", e);

            if (baseUri != null)
                throw new Exception($"Failed to reach {relativePath} from {baseUri.AbsoluteUri}", e);
        }
    }

    public override TicketingReleaseabilityEvaluation GetDataReleaseabilityOfTicket(string masterTicket, string requestTicket, string releaseTicket, out string reason, out Exception exception)
    {
        exception = null;
        try
        {
            SetupIfRequired();
        }
        catch (Exception e)
        {
            reason = "Failed to setup a connection to the JIRA API";
            exception = e;
            return TicketingReleaseabilityEvaluation.TicketingLibraryMissingOrNotConfiguredCorrectly;
        }

        //make sure JIRA data is configured correctly
        if (string.IsNullOrWhiteSpace(masterTicket))
        {
            reason = "Master JIRA ticket is blank";
            return TicketingReleaseabilityEvaluation.NotReleaseable;
        }

        if(string.IsNullOrWhiteSpace(requestTicket))
        {
            reason = "Request JIRA ticket is blank";
            return TicketingReleaseabilityEvaluation.NotReleaseable;
        }

        if (string.IsNullOrWhiteSpace(releaseTicket))
        {
            reason = "Release JIRA ticket is blank";
            return TicketingReleaseabilityEvaluation.NotReleaseable;
        }

        //Get status of tickets from JIRA API
        try
        {
            JIRAReleaseTicketStatus = GetStatusOfJIRATicket(releaseTicket);
            GetAttachementsOfJIRATicket(requestTicket);
        }
        catch (Exception e)
        {
            reason = "Problem occurred getting the status of the release ticket or the attachemnts stored under the request ticket";
            exception = e;
            return e.Message.Contains("Authentication Required") ? TicketingReleaseabilityEvaluation.CouldNotAuthenticateAgainstServer : TicketingReleaseabilityEvaluation.CouldNotReachTicketingServer;
                
        }

        //if it isn't at required status
        if (!PermissableReleaseStatusesForJIRAReleaseTickets.Contains(JIRAReleaseTicketStatus))
        {
            reason =
                $"Status of release ticket ({JIRAReleaseTicketStatus}) was not one of the permissable release ticket statuses: {string.Join(",", PermissableReleaseStatusesForJIRAReleaseTickets)}";

            return TicketingReleaseabilityEvaluation.NotReleaseable; //it cannot be released
        }

        if (!JIRAProjectAttachements.Any(a => a.filename.EndsWith(".docx") || a.filename.EndsWith(".doc")))
        {
            reason =
                $"Request ticket {requestTicket} must have at least one Attachment with the extension .doc or .docx ";

            if (JIRAProjectAttachements.Any())
                reason +=
                    $". Current attachments were: {string.Join(",", JIRAProjectAttachements.Select(a => a.filename).ToArray())}";

            return TicketingReleaseabilityEvaluation.NotReleaseable;
        }

        reason = null;
        return TicketingReleaseabilityEvaluation.Releaseable;
    }

    public override string GetProjectFolderName(string masterTicket)
    {
        SetupIfRequired();
        var issue = _client.GetIssue(masterTicket, new[] {"summary", "customfield_13400"});

        return issue.fields.customfield_13400;
    }

    private void SetupIfRequired()
    {
        if (_client != null)
            return;
            
        _client = new JiraClient(new JiraAccount(new JiraApiConfiguration()
        {
            ServerUrl = Url,
            User = Credentials.Username,
            Password = Credentials.GetDecryptedPassword()
        }));
    }

    private string GetStatusOfJIRATicket(string ticket)
    {
        var issue = GetIssue(ticket);

        if (issue == null)
            throw new Exception($"Non existent ticket: {ticket}");

        return issue.fields.status.name;
    }


    private void GetAttachementsOfJIRATicket(string ticket)
    {
        var issue = GetIssue(ticket);
           
        if (issue == null)
            throw new Exception($"Non existent ticket: {ticket}");

        JIRAProjectAttachements = issue.fields.attachment;
    }

    private Issue GetIssue(string ticket)
    {
        return _client.GetIssue(ticket);
    }

}