using System.Data.SqlClient;
using System.Reflection;
using CatalogueLibrary.Data;
using HIC.Logging;
using MapsDirectlyToDatabaseTable.Versioning;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using Tests.Common;

namespace HICPluginTests.Integration
{
    public class PluginDatabaseTests : DatabaseTests
    {
        private bool CheckDatabaseExists(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "";

            var server = new DiscoveredServer(builder);
            return server.ExpectDatabase(databaseName).Exists();
        }

        private void CreateOrPatchIfRequired(string connectionString, Assembly databaseAssembly)
        {
            var databaseManager = new MasterDatabaseScriptExecutor(connectionString);
            if (CheckDatabaseExists(connectionString))
            {
                var patches = Patch.GetAllPatchesInAssembly(databaseAssembly);
                databaseManager.PatchDatabase(patches, new AcceptAllCheckNotifier(), p => true);
            }
            else
            {
                databaseManager.CreateAndPatchDatabaseWithDotDatabaseAssembly(databaseAssembly, new AcceptAllCheckNotifier());
            }
        }

        protected override void SetUp()
        {
            // We want control over setting up the catalogue connections as we may have to create databases in the first place
            SetUpCatalogueConnections = false;
            base.SetUp();

            var catalogueConnectionString = GetValueFromAppConfigSuperAwesome("TestCatalogueConnectionString");
            var catalogueExists = CheckDatabaseExists(catalogueConnectionString);
            CreateOrPatchIfRequired(catalogueConnectionString, typeof(CatalogueLibraryDatabase.Class1).Assembly);
            DatabaseSettings.SetCatalogueConnectionString(catalogueConnectionString);

            var dataExportManagerConnectionString = GetValueFromAppConfigSuperAwesome("DataExportManagerConnectionString");
            CreateOrPatchIfRequired(dataExportManagerConnectionString, typeof(DataExportManager2Database.Class1).Assembly);
            DatabaseSettings.ConnectionConfiguration.DataExportManagerConnectionString = dataExportManagerConnectionString;

            //DatabaseSettings are not valid for these, DatabaseSettings tells you where to find the master databases i.e. DataCatalogue tells you where to find everything else via it's ExternalDatabaseServer table
            UnitTestLoggingConnectionString = new SqlConnectionStringBuilder(GetValueFromAppConfigSuperAwesome("UnitTestLoggingConnectionString"));
            CreateOrPatchIfRequired(UnitTestLoggingConnectionString.ConnectionString, typeof(HIC.Logging.Database.Class1).Assembly);

            // Add DQE database details into catalogue as an ExternalDatabaseServer
            var dataQualityEngineConnectionString = GetValueFromAppConfigSuperAwesome("DataQualityEngineConnectionString");
            CreateOrPatchIfRequired(dataQualityEngineConnectionString, typeof(DataQualityEngine.Database.Class1).Assembly);
            SetupDQEReportingDatabase(dataQualityEngineConnectionString);

            //run logging checks
            var loggingChecker = new LoggingDatabaseChecker(new DiscoveredServer(UnitTestLoggingConnectionString));
            loggingChecker.Check(new AcceptAllCheckNotifier());
        }
    }
}