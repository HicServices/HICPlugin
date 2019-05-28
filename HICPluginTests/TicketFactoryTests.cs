using JiraPlugin;
using NUnit.Framework;
using Rdmp.Core.Ticketing;
using ReusableLibraryCode.DataAccess;
using Rhino.Mocks;
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
            var credentials = MockRepository.GenerateStub<IDataAccessCredentials>();
            Assert.DoesNotThrow(() => factory.Create(typeof(JIRATicketingSystem).FullName, "Bob", credentials));
        }

    }
}
