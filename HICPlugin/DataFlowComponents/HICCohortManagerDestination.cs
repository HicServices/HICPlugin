using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using DataExportLibrary.Interfaces.Pipeline;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataTableExtension;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    public class HICCohortManagerDestination : IPluginCohortDestination
    {
        [DemandsInitialization("The name of the stored proceedure which will commit entirely new cohorts")] 
        public string NewCohortsStoredProceedure { get; set; }

        [DemandsInitialization("The name of the stored proceedure which will augment existing cohorts with new versions")]
        public string ExistingCohortsStoredProceedure { get; set; }

        public ICohortCreationRequest Request { get; set; }

        public DataTable AllAtOnceDataTable;
        private string _privateIdentifier;

        Stopwatch sw = new Stopwatch();

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
        {

            sw.Start();
            if (AllAtOnceDataTable == null)
            {
                if(!toProcess.Columns.Contains(_privateIdentifier))
                    throw new Exception("Pipeline did not have a column called "+ _privateIdentifier);

                AllAtOnceDataTable = new DataTable("CohortUpload_HICCohortManagerDestination");
                AllAtOnceDataTable.Columns.Add(_privateIdentifier);
            }

            foreach (DataRow dr in toProcess.Rows)
                AllAtOnceDataTable.Rows.Add(dr[_privateIdentifier]);

            listener.OnProgress(this,new ProgressEventArgs("Buffering all identifiers in memory",new ProgressMeasurement(AllAtOnceDataTable.Rows.Count,ProgressType.Records),sw.Elapsed));
            sw.Stop();

            return null;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to send all the buffered data up to the server"));

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var target = Request.NewCohortDefinition.LocationOfCohort;
            var discoveredDatabase = target.GetExpectDatabase();

            DataTableHelper helper = new DataTableHelper(AllAtOnceDataTable);

            string tempTable = helper.CommitDataTableToTempDB(discoveredDatabase.Server, false);
            tempTable = "tempdb.." + tempTable;
            try
            {
                sw.Stop();
                listener.OnProgress(this, new ProgressEventArgs("Uploading to "+tempTable,new ProgressMeasurement(AllAtOnceDataTable.Rows.Count,ProgressType.Records), sw.Elapsed));

                //commit from temp table (most likely place to crash)
                using (SqlConnection con = (SqlConnection) discoveredDatabase.Server.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd;
                    var transaction = con.BeginTransaction("Committing cohort");

                    if (Request.NewCohortDefinition.Version == 1)
                    {

                        cmd = new SqlCommand(NewCohortsStoredProceedure, con, transaction);
                        cmd.Parameters.AddWithValue("sourceTableName", tempTable);
                        cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                        cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
                    }

                    
                    else
                    {
                        //get the existing cohort number 
                        var cmdGetCohortNumber = new SqlCommand("(SELECT MAX(cohortNumber) FROM " + target.DefinitionTableName +
                                              " where description = '" + Request.NewCohortDefinition.Description + "')" , con,transaction);
                        var cohortNumber = Convert.ToInt32(cmdGetCohortNumber.ExecuteScalar());

                        //call the commit
                        cmd = new SqlCommand(ExistingCohortsStoredProceedure,con,transaction);
                        cmd.Parameters.AddWithValue("sourceTableName", tempTable);
                        cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                        cmd.Parameters.AddWithValue("cohortNumber", cohortNumber);
                        cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
                    }

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 100000;

                    var ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);


                    foreach (DataTable dt in ds.Tables)
                    {
                        var str = string.Join(",", dt.Columns.Cast<DataColumn>().Select(c=>c.ColumnName).ToArray());

                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, NewCohortsStoredProceedure + " said:" + str));

                        foreach (DataRow dr in dt.Rows)
                            listener.OnNotify(this,
                                new NotifyEventArgs(ProgressEventType.Warning,
                                    NewCohortsStoredProceedure + " said:" + string.Join(",", dr.ItemArray)));
                    }

                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Called stored proceedure " + cmd.CommandText));
                    
                    transaction.Commit();
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Finished data load and comitted transaction" + cmd.CommandText));
                }
            }
            finally
            {
                //clean up the temp table
                using (SqlConnection con = (SqlConnection) discoveredDatabase.Server.GetConnection())
                {
                    con.Open();
                    SqlCommand cmdDropTempTable = new SqlCommand("DROP TABLE " + tempTable, con);
                    cmdDropTempTable.ExecuteNonQuery();
                    con.Close();
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Dropped temp table " + tempTable));
                }
                
            }

        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public void PreInitialize(ICohortCreationRequest value, IDataLoadEventListener listener)
        {
            Request = value;
            _privateIdentifier = SqlSyntaxHelper.GetRuntimeName(Request.NewCohortDefinition.LocationOfCohort.PrivateIdentifierField);
        }

        public void Check(ICheckNotifier notifier)
        {

            var location = Request.NewCohortDefinition.LocationOfCohort;

            //check the cohort database
            location.Check(notifier);

            //now check the stored procs it has in it
            var builder = (SqlConnectionStringBuilder) location.GetExpectDatabase().Server.Builder;
            var spsFound = UsefulStuff.GetInstance().ListStoredProcedures(builder.ConnectionString, builder.InitialCatalog);

            //have they forgotten to tell us what the proc is?
            if (string.IsNullOrEmpty(NewCohortsStoredProceedure))
                notifier.OnCheckPerformed(new CheckEventArgs("DemandsInitialization property NewCohortsStoredProceedure is blank",CheckResult.Fail));
                else
                    if (spsFound.Contains(NewCohortsStoredProceedure))//it exists!
                        notifier.OnCheckPerformed(new CheckEventArgs("Found stored proceedure " + NewCohortsStoredProceedure + " in cohort database " + location, CheckResult.Success)); 
                    else //it doesnt exist!
                        notifier.OnCheckPerformed(new CheckEventArgs("Could not find stored proceedure " + NewCohortsStoredProceedure + " in cohort database " + location, CheckResult.Fail));

            //now do the same again for ExistingCohortsStoredProceedure
            if (string.IsNullOrEmpty(ExistingCohortsStoredProceedure))
                notifier.OnCheckPerformed(
                    new CheckEventArgs("DemandsInitialization property ExistingCohortsStoredProceedure is blank",
                        CheckResult.Fail));
                else
                    if (spsFound.Contains(ExistingCohortsStoredProceedure))
                        notifier.OnCheckPerformed(new CheckEventArgs("Found stored proceedure " + ExistingCohortsStoredProceedure + " in cohort database " + location, CheckResult.Success));
                    else
                        notifier.OnCheckPerformed(new CheckEventArgs("Could not find stored proceedure " + ExistingCohortsStoredProceedure + " in cohort database " + location, CheckResult.Fail));


        }
    }
}
