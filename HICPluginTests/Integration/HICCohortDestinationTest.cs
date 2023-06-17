using System;
using System.Data;
using HICPlugin.DataFlowComponents;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using NUnit.Framework;
using Rdmp.Core.CohortCommitting.Pipeline;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;

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
        var p = new Project(RepositoryLocator.DataExportRepository, "p");
        p.ProjectNumber = projectNumberInTestData;
        p.SaveToDatabase();

        //delete RDMP knowledge of the cohort
        ((IDeleteable)_extractableCohort).DeleteInDatabase();

        var discoveredServer = _externalCohortTable.Discover().Server;
        using (var con = discoveredServer.GetConnection())
        {
            con.Open();

            var sql = _expectToSucceed ? $@"
if exists(select * from sys.procedures where name = 'fishfishfishproc1')
	begin
		drop procedure fishfishfishproc1
	end
GO

create procedure fishfishfishproc1
                    @sourceTableName varchar(10),
                    @projectNumber int,
                    @description varchar(10)
                    as
                    begin
                        select distinct id from {definitionTableName} 
                    end" : @"
if exists(select * from sys.procedures where name = 'fishfishfishproc1')
	begin
		drop procedure fishfishfishproc1
	end
GO

                    create procedure fishfishfishproc1
                    @sourceTableName varchar(10),
                    @projectNumber int,
                    @description varchar(10)
                    as
                    begin
                        select 0
                    end";
            UsefulStuff.ExecuteBatchNonQuery(sql,con);
        }
            
        var def = new CohortDefinition(null, "ignoremecohort", 1, 12,_externalCohortTable);
        var request = new CohortCreationRequest(p, def, (DataExportRepository)RepositoryLocator.DataExportRepository, "ignoremeauditlog");

        var d = new HICCohortManagerDestination
        {
            NewCohortsStoredProcedure = "fishfishfishproc1",
            ExistingCohortsStoredProcedure = "fishfishfishproc2"
        };
        d.PreInitialize(request,ThrowImmediatelyDataLoadEventListener.Quiet);
        d.CreateExternalCohort = true;

        var dt = new DataTable("mytbl");
        dt.Columns.Add("PrivateID");
        dt.Rows.Add("101");
        dt.Rows.Add("102");

        d.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
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