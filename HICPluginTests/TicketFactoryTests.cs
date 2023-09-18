using JiraPlugin;
using NUnit.Framework;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using Tests.Common;
using NSubstitute;

namespace JiraPluginTests;

[TestFixture]
public class TicketFactoryTests : DatabaseTests
{
    [Test]
    public void FactoryKnowsAboutJIRA()
    {
        Assert.Contains(typeof (JIRATicketingSystem), TicketingSystemFactory.GetAllKnownTicketingSystems());
    }


    [Test]
    public void FactoryCreateAJIRA()
    {
        var credentials = Substitute.For<IDataAccessCredentials>();
        Assert.DoesNotThrow(() => TicketingSystemFactory.Create(typeof(JIRATicketingSystem).FullName, "Bob", credentials));
    }

}