using JiraPlugin;
using NUnit.Framework;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using Tests.Common;

namespace JiraPluginTests
{
    [TestFixture]
    public class TicketFactoryTests : DatabaseTests
    {
        [Test]
        public void FactoryKnowsAboutJIRA()
        {
            var factory = new TicketingSystemFactory(CatalogueRepository);
            Assert.Contains(typeof (JIRATicketingSystem), factory.GetAllKnownTicketingSystems());
        }


        [Test]
        public void FactoryCreateAJIRA()
        {
            var factory = new TicketingSystemFactory(CatalogueRepository);
            var credentials = new Moq.Mock<IDataAccessCredentials>().Object;
            Assert.DoesNotThrow(() => factory.Create(typeof(JIRATicketingSystem).FullName, "Bob", credentials));
        }

    }
}
