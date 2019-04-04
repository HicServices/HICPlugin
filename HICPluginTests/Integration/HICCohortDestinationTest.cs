using System;
using System.Data;
using CatalogueLibrary.DataFlowPipeline;
using DataExportLibrary.CohortCreationPipeline;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Repositories;
using DataExportLibrary.Tests;
using HICPlugin.DataFlowComponents;
using MapsDirectlyToDatabaseTable;
using NUnit.Framework;
using ReusableLibraryCode.Progress;

namespace HICPluginTests.Integration
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class HICCohortDestinationTest : TestsRequiringACohort
    {
        private bool _expectToSucceed;

        public HICCohortDestinationTest(bool expectToSucceed)
        {
            _expectToSucceed = expectToSucceed;
        }

        [Test]
        public void UploadToTarget()
        {
            Project p = new Project(RepositoryLocator.DataExportRepository, "p");
            p.ProjectNumber = projectNumberInTestData;
            p.SaveToDatabase();

            //delete RDMP knowledge of the cohort
            ((IDeleteable)_extractableCohort).DeleteInDatabase();

            var discoveredServer = _externalCohortTable.Discover().Server;
            using (var con = discoveredServer.GetConnection())
            {
                con.Open();

                string sql;
                if (_expectToSucceed)
                {
                    sql = string.Format(@"create procedure fishfishfishproc1
                    @sourceTableName varchar(10),
                    @projectNumber int,
                    @description varchar(10)
                    as
                    begin
                        select distinct id from {0} 
                    end", definitionTableName);
                }
                else
                {
                    sql = string.Format(@"create procedure fishfishfishproc1
                    @sourceTableName varchar(10),
                    @projectNumber int,
                    @description varchar(10)
                    as
                    begin
                        select 0
                    end");
                }
                
                discoveredServer.GetCommand(sql, con).ExecuteNonQuery();
            }
            
            var def = new CohortDefinition(null, "ignoremecohort", 1, 12,_externalCohortTable);
            var request = new CohortCreationRequest(p, def, (DataExportRepository)RepositoryLocator.DataExportRepository, "ignoremeauditlog");

            var d = new HICCohortManagerDestination();
            d.NewCohortsStoredProceedure = "fishfishfishproc1";
            d.ExistingCohortsStoredProceedure = "fishfishfishproc2";
            d.PreInitialize(request,new ThrowImmediatelyDataLoadEventListener());
            d.CreateExternalCohort = true;

            var dt = new DataTable("mytbl");
            dt.Columns.Add("PrivateID");
            dt.Rows.Add("101");
            dt.Rows.Add("102");

            d.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
            var tomem = new ToMemoryDataLoadEventListener(false);

            if (_expectToSucceed)
            {
                //actually does the send
                d.Dispose(tomem,null);

                Assert.AreEqual(cohortIDInTestData, request.CohortCreatedIfAny.OriginID);
            }
            else
                Assert.Throws<Exception>(() => d.Dispose(tomem, null));

        }
    }
}
