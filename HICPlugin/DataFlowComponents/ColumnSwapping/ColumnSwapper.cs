using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataHelper;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.DataTableExtension;
using ReusableLibraryCode.Progress;


namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    [Description("Performs 1 to 1 value substitutions based on a set of rules.")]
    public class ColumnSwapper:IPluginDataFlowComponent<DataTable>
    {
        [DemandsInitialization("Contains the rules that will be used to establish a 1 to 1 mapping between input columns and mapped values e.g. Input.Forename = MappingTable.Forename, select AnonForename.")]
        public ColumnSwapConfiguration Configuration{ get; set; }

        [DemandsInitialization("By default the swapper will find a 1 to 1 mapping and add a new column to the pipeline that replaces the old (redundant) column e.g. privateId=>ReleaseId.  If you set this to true then the new column will be added but the old column will also remain in the pipeline")]
        public bool DoNotDropOriginalColumn { get; set; }


        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener job, GracefulCancellationToken cancellationToken)
        {
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "Preparing to bulk insert into tempdb on server " + Configuration.Server));
           
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(){DataSource = Configuration.Server,InitialCatalog = "tempdb",IntegratedSecurity = true};
            SqlConnection conToTempDb = new SqlConnection(builder.ConnectionString);
            conToTempDb.Open();
            
            DataTableHelper upload = new DataTableHelper(toProcess);

            DiscoveredServer server = new DiscoveredServer(builder);

            string uploadedName = upload.CommitDataTableToTempDB(server,conToTempDb,Configuration.UseOldDateTimes);
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "DataTable to bulk insert into tempdb with table name " +uploadedName + " on server " + Configuration.Server ));

            SubstitutionRule.SubstitutionResult result =  SubstitutionRule.CheckRules(Configuration.Rules,uploadedName,Configuration.MappingTableName,Configuration.ColumnToPerformSubstitutionOn,Configuration.SubstituteColumn,conToTempDb,Configuration.Timeout);
            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "DataTable to bulk insert into tempdb with table name " +uploadedName + " on server " + Configuration.Server ));

            if(result == null)
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "SubstitutionRule.CheckRules returned null, most likely there are no rules configured"));

            if(result.IsExactlyOneToOne(Configuration.AllowMto1Errors,Configuration.Allow1ToZeroErrors))
            {
                if(result.ManyToOneErrors > 0)
                    job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "There were " + result.OneToManyErrors + " 1 to Many errors (mapping of 2 different input identifiers to the same output identifier, Your current configuration allows this which may be desired e.g. if you were trying to substitute forename into first initial)"));

                if (result.OneToZeroErrors > 0)
                    job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "There were " + result.OneToZeroErrors + " 1 to 0 errors (identifiers that do not map to anything and have thus been discarded - will appear as NULL.  Your current configuration allows this which is NOT RECOMMENDED.  At the very least you should contact the file supplier and notify them of the unknown identifiers)"));

                var dtToReturn = ApplyUPDATE(
                    uploadedName,
                    Configuration.MappingTableName,
                    Configuration.ColumnToPerformSubstitutionOn,
                    Configuration.SubstituteColumn,
                    GetSubstituteForInMappingTableDataType(builder),
                    conToTempDb,
                    Configuration.Timeout,
                    job);


                //return it with the same name as it came in with
                dtToReturn.TableName = toProcess.TableName;

                return dtToReturn;
            }

            job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error,"Failed to get 1 to 1 mapping, mapping results were " + result));
            
            throw new Exception("Abandonning pipeline because OneToOne identifier mapping was not obtained");
        }

        private string GetSubstituteForInMappingTableDataType(SqlConnectionStringBuilder builder)
        {
            //discover the mapping table
            var table = new DiscoveredServer(builder).ExpectDatabase(Configuration.Database).ExpectTable(Configuration.MappingTableName);

            if(!table.Exists())
                throw new Exception("Could not find mapping table "+ table);
            
            return table.DiscoverColumn(Configuration.SubstituteColumn).DataType.SQLType;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            
        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public DataTable ApplyUPDATE(string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable, string substituteForInMappingTableDataType, SqlConnection conToSourceTable, int timeout, IDataLoadEventListener job)
        {
            SqlTransaction transaction = conToSourceTable.BeginTransaction();


            //add the new column
            string sql = "";

            sql = "ALTER TABLE " + sqlOriginTable + " ADD " + RDMPQuerySyntaxHelper.EnsureValueIsWrapped(substituteForInMappingTable) + " " + substituteForInMappingTableDataType;

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Adding new column to DataTable in tempdb:" + sql));
            
            SqlCommand cmd = new SqlCommand(sql, conToSourceTable);
            cmd.Transaction = transaction;
            cmd.ExecuteNonQuery();

            //do the update
            string andStatement = string.Join(Environment.NewLine + " AND ", Configuration.Rules.Select(r => r.GetWhereSql()));
            

            sql = string.Format(@"update source 
  set source.{0}  = map.{0}
  from
  (
  {1} source join {2} map
  on
  {3}
  )"
                , substituteForInMappingTable
                , sqlOriginTable
                , sqlMappingTable
                , andStatement);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,  "About to update the newly created column in tempdb to have the established 1-1 substitution identifiers using the following SQL:" + sql));
            
            cmd = new SqlCommand(sql, conToSourceTable);
            cmd.Transaction = transaction;
            cmd.CommandTimeout = timeout;

            cmd.ExecuteNonQuery();


            if(!DoNotDropOriginalColumn)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Dropping old column " + substituteInSourceColumn + " because substitution has been succesful"));
                cmd = new SqlCommand(
                    string.Format("alter table {0} drop column {1}"
                        , sqlOriginTable
                        , substituteInSourceColumn), conToSourceTable);
            }

            cmd.Transaction = transaction;

            cmd.ExecuteNonQuery();
            

            transaction.Commit();

            var result = new DataTable();

            SqlCommand cmdDownload = new SqlCommand("SELECT * FROM " + sqlOriginTable, conToSourceTable);
            
            SqlDataAdapter da = new SqlDataAdapter(cmdDownload);
            da.Fill(result);
            
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Dropping temp table " + sqlOriginTable + " because substitution has been succesful"));

            if (!conToSourceTable.Database.Equals("tempdb"))
                throw new Exception("When did we stop being pointed at tempdb, this is bad times!(we were about to drop " + sqlOriginTable + " in database "+conToSourceTable.Database+")?");

            SqlCommand cmdDropTempTable = new SqlCommand("DROP TABLE " + sqlOriginTable, conToSourceTable);
            cmdDropTempTable.ExecuteNonQuery();
            
            return result;
        }
        

        public void Check(ICheckNotifier notifier)
        {
            if (Configuration == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Configuration property has not been set", CheckResult.Fail, null));
            else
                Configuration.Check(notifier);
        }

     
    }
}
