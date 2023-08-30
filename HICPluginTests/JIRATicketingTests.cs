using JiraPlugin;
using NUnit.Framework;
using Rdmp.Core.Ticketing;

namespace JiraPluginTests;

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
        var j = new JIRATicketingSystem(new TicketingSystemConstructorParameters("", null));
        Assert.IsTrue(j.IsValidTicketName(pattern));
    }
}