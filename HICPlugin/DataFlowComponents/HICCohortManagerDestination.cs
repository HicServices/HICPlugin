using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using CatalogueLibrary.CohortCreation;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.DataFlowPipeline.Destinations;
using FAnsi.Discovery;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
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
        public bool CreateExternalCohort { get; set; }

        public DataTable AllAtOnceDataTable;
        private string _privateIdentifier;

        Stopwatch sw = new Stopwatch();

        public HICCohortManagerDestination()
        {
            CreateExternalCohort = true;
        }

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
            var cohortDatabase = target.Discover();

            string tempTableName = QuerySyntaxHelper.MakeHeaderNameSane(Guid.NewGuid().ToString());
            AllAtOnceDataTable.TableName = tempTableName;

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Uploading to " + tempTableName));

            var dest = new DataTableUploadDestination();

            if(_privateIdentifier.Equals("chi",StringComparison.CurrentCultureIgnoreCase))
                dest.AddExplicitWriteType(_privateIdentifier, "varchar(10)");

            dest.AllowResizingColumnsAtUploadTime = true;
            dest.PreInitialize(cohortDatabase,listener);
            dest.ProcessPipelineData(AllAtOnceDataTable, listener, new GracefulCancellationToken());
            dest.Dispose(listener,null);

            var tbl = cohortDatabase.ExpectTable(tempTableName);
            if(!tbl.Exists())
                throw new Exception("Temp table '" + tempTableName + "' did not exist in cohort database '" + cohortDatabase +"'");
            try
            {

                //commit from temp table (most likely place to crash)
                using (SqlConnection con = (SqlConnection) cohortDatabase.Server.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd;
                    var transaction = con.BeginTransaction("Committing cohort");

                    if (Request.NewCohortDefinition.Version == 1)
                    {
                        cmd = new SqlCommand(NewCohortsStoredProceedure, con, transaction);
                        cmd.Parameters.AddWithValue("sourceTableName", tbl.GetFullyQualifiedName());
                        cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                        cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
                    }
                    else
                    {
                        //get the existing cohort number 
                        var cmdGetCohortNumber =
                            new SqlCommand("(SELECT MAX(cohortNumber) FROM " + target.DefinitionTableName +
                                           " where description = '" + Request.NewCohortDefinition.Description + "')",
                                con, transaction);
                        var cohortNumber = Convert.ToInt32(cmdGetCohortNumber.ExecuteScalar());

                        //call the commit
                        cmd = new SqlCommand(ExistingCohortsStoredProceedure, con, transaction);
                        cmd.Parameters.AddWithValue("sourceTableName", tbl.GetFullyQualifiedName());
                        cmd.Parameters.AddWithValue("projectNumber", Request.Project.ProjectNumber);
                        cmd.Parameters.AddWithValue("cohortNumber", cohortNumber);
                        cmd.Parameters.AddWithValue("description", Request.NewCohortDefinition.Description);
                    }

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 100000;

                    var cohortId = Convert.ToInt32(cmd.ExecuteScalar());
                    
                    listener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Information, "Called stored proceedure " + cmd.CommandText));

                    if (cohortId == 0)
                        throw new Exception("Stored procedure returned null or 0");
                    
                    transaction.Commit();
                    listener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Information,
                            "Finished data load and comitted transaction" + cmd.CommandText));

                    if (CreateExternalCohort)
                    {
                        Request.NewCohortDefinition.ID = cohortId;
                        listener.OnNotify(this,
                            new NotifyEventArgs(ProgressEventType.Information,
                                "About to attempt to create a pointer to this cohort that has been created"));
                        Request.ImportAsExtractableCohort(true);
                        listener.OnNotify(this,
                            new NotifyEventArgs(ProgressEventType.Information,
                                "Succesfully created pointer, you should now have access to your cohort in RDMP"));
                    }
                }
            }
            finally
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Dropping " + tbl.GetFullyQualifiedName()));
                tbl.Drop();
            }
        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public void PreInitialize(ICohortCreationRequest value, IDataLoadEventListener listener)
        {
            Request = value;
            var syntaxHelper = value.NewCohortDefinition.LocationOfCohort.GetQuerySyntaxHelper();
            _privateIdentifier = syntaxHelper.GetRuntimeName(Request.NewCohortDefinition.LocationOfCohort.PrivateIdentifierField);
        }

        public void Check(ICheckNotifier notifier)
        {

            var location = Request.NewCohortDefinition.LocationOfCohort;

            //check the cohort database
            location.Check(notifier);

            //now check the stored procs it has in it
            var spsFound = location.Discover().DiscoverStoredprocedures().Select(sp => sp.Name).ToArray();

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
