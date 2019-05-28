using System;
using NUnit.Framework;
using ReusableLibraryCode.DataAccess;

namespace JiraPluginTests
{
    [TestFixture]
    public class JIRATicketingTests
    {
        [Test]
        [TestCase("LINK-123")]
        [TestCase("DMW-123")]
        [TestCase("DWP-123420023")]
        [TestCase("HDR-001")]
        [TestCase("")]//blank is valid
        public void JIRARegex_Passes(string pattern)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("",null));
            Assert.IsTrue(j.IsValidTicketName(pattern));
        }

        [Test]
        [TestCase("LINK-2377")]
        public void JIRA_GetSafeHavenFolder_OK(string masterTicket)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("https://jira-hic-test.cmdn.dundee.ac.uk/", new TestJiraCredentials()));
            Assert.That(j.GetProjectFolderName(masterTicket), Is.EqualTo("/LovelyCoconuts project")); //
        }

        [Test]
        [TestCase("LIN123")]
        [TestCase("123")]
        [TestCase("123-123420023")]
        public void JIRARegex_Fails(string pattern)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("", null));
            Assert.IsFalse(j.IsValidTicketName(pattern));
        }

        [Test]
        [TestCase("LINK-2377", "LINK-2378", "LINK-2403")]
        public void JIRA_Ticket_Releasable(string masterTicket, string request, string release)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("https://jira-hic-test.cmdn.dundee.ac.uk/", new TestJiraCredentials()));
            string reason;
            Exception ex;
            var releasability = j.GetDataReleaseabilityOfTicket(masterTicket, request, release, out reason, out ex);
            Assert.That(ex, Is.Null);
            Assert.That(releasability, Is.EqualTo(TicketingReleaseabilityEvaluation.Releaseable));
        }

        [Test]
        [TestCase("LINK-2377", "LINK-2378", "LINK-2402")]
        public void JIRA_Ticket_NonReleasable(string masterTicket, string request, string release)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("https://jira-hic-test.cmdn.dundee.ac.uk/", new TestJiraCredentials()));
            string reason;
            Exception ex;
            var releasability = j.GetDataReleaseabilityOfTicket(masterTicket, request, release, out reason, out ex);
            Assert.That(ex, Is.Null);
            Assert.That(releasability, Is.EqualTo(TicketingReleaseabilityEvaluation.NotReleaseable));
            Assert.That(reason, Does.StartWith("Status of release ticket (In Progress)"));
        }

        [Test]
        [TestCase("LINK-2377", "LINK-2404", "LINK-2406")]
        public void JIRA_Ticket_NoAttachments(string masterTicket, string request, string release)
        {
            var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("https://jira-hic-test.cmdn.dundee.ac.uk/", new TestJiraCredentials()));
            string reason;
            Exception ex;
            var releasability = j.GetDataReleaseabilityOfTicket(masterTicket, request, release, out reason, out ex);
            Assert.That(ex, Is.Null);
            Assert.That(releasability, Is.EqualTo(TicketingReleaseabilityEvaluation.NotReleaseable));
            Assert.That(reason, Does.StartWith("Request ticket " + request + " must have at least one Attachment"));
        }
    }

    public class TestJiraCredentials : IDataAccessCredentials
    {
        public string GetDecryptedPassword()
        {
            return "J1r@4piU$er";
        }

        public string Password { get; set; }

        public string Username
        {
            get { return "api"; }
        }
    }
}
