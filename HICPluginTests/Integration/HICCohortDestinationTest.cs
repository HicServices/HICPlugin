using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.DataFlowPipeline;
using DataExportLibrary.CohortCreationPipeline;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Repositories;
using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace HICPluginTests.Integration
{
    public class HICCohortDestinationTest : DatabaseTests
    {
        [Test]
        public void UploadToTarget()
        {
            Project p = new Project(RepositoryLocator.DataExportRepository, "p");
            p.ProjectNumber = 12;
            p.SaveToDatabase();
            
            ExternalCohortTable t = new ExternalCohortTable(RepositoryLocator.DataExportRepository,"CohortDatabase");
            t.Server = DiscoveredDatabaseICanCreateRandomTablesIn.Server.Name;
            t.Database = DiscoveredDatabaseICanCreateRandomTablesIn.GetRuntimeName();
            t.PrivateIdentifierField = "myidents";
            t.SaveToDatabase();

            var s = DiscoveredDatabaseICanCreateRandomTablesIn.Server;
            using (var con = s.GetConnection())
            {
                con.Open();

                s.GetCommand(@"create procedure fishfishfishproc1

@sourceTableName varchar(10),
@projectNumber int,
@description varchar(10)
as
begin
select 1

end
", con).ExecuteNonQuery();

            }

            var def = new CohortDefinition(null, "ignoremecohort", 1, 12, t);
            var request = new CohortCreationRequest(p, def, (DataExportRepository)RepositoryLocator.DataExportRepository, "ignoremeauditlog");

            var d = new HICCohortManagerDestination();
            d.NewCohortsStoredProceedure = "fishfishfishproc1";
            d.ExistingCohortsStoredProceedure = "fishfishfishproc2";
            d.PreInitialize(request,new ToConsoleDataLoadEventReceiver());

            var dt = new DataTable("mytbl");
            dt.Columns.Add("myidents");
            dt.Rows.Add("101");
            dt.Rows.Add("102");

            d.ProcessPipelineData(dt, new ToConsoleDataLoadEventReceiver(), new GracefulCancellationToken());
            var tomem = new ToMemoryDataLoadEventReceiver(true);
            //actually does the send
            d.Dispose(tomem,null);
            
            
            Assert.IsTrue(tomem.EventsReceivedBySender.Any(v=>v.Value.Any(e=>e.Message.Equals("fishfishfishproc1 said:1"))));

        }
    }
}
