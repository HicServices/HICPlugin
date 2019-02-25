using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine;
using DataLoadEngine.Attachers;
using DataLoadEngine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;

namespace HICPlugin.Microbiology
{
    public class MicrobiologyAttacher : Attacher, IPluginAttacher
    {

        [DemandsInitialization("The 'header' table which contains all the lab details e.g. CHI, SampleDate, Clinician etc")]
        public TableInfo LabTable { get; set; }
        
        [DemandsInitialization("The 'results' table which contains all the different results for each header lab details (TestCode and ResultCode)")]
        public TableInfo TestsTable { get; set; }

        [DemandsInitialization("The table which contains all the specimens which are isolations???")]
        public TableInfo IsolationsTable { get; set; }

        [DemandsInitialization("The table which contains all the isolation results")]
        public TableInfo IsolationResultsTable { get; set; }

        [DemandsInitialization("The table which contains all the specimens which are NOT isolations???")]
        public TableInfo NoIsolationsTable { get; set; }

        List<MB_Tests> Tests = new List<MB_Tests>();
        List<MB_Lab> Labs = new List<MB_Lab>();
        List<MB_NoIsolations> NoIsolations = new List<MB_NoIsolations>();
        List<MB_IsolationResult> IsolationResults = new List<MB_IsolationResult>();
        List<MB_Isolation> Isolations = new List<MB_Isolation>();

        [DemandsInitialization("The file(s) to attach e.g. *.txt, this is NOT a REGEX")]
        public string FilePattern { get; set; }

        public MicrobiologyAttacher():base(true)
        {
            
        }

        public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
            
        }

        private Dictionary<Type,PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>(); 
        private Dictionary<TableInfo, DataTable> _dataTables = new Dictionary<TableInfo, DataTable>();
        private Dictionary<PropertyInfo,int>  _lengthsDictionary = new Dictionary<PropertyInfo, int>();
        private IDataLoadJob _currentJob;


        public override ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
        {
            _currentJob = job;

            SetupPropertyBasedReflectionIntoDataTables(null);
            SetupDataTables();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            int recordCount = 0;

            foreach (var fileToLoad in LoadDirectory.ForLoading.EnumerateFiles(FilePattern))
            {
                MicroBiologyFileReader r = new MicroBiologyFileReader(fileToLoad.FullName);
                r.Warning += r_Warning;
                try
                {
                    foreach (IMicrobiologyResultRecord result in r.ProcessFile())
                    {
                        //header records
                        if (result is MB_Lab)
                            AddResultToDataTable(_dataTables[LabTable],result);
                        
                        //things that were isolated
                        if (result is MB_Isolation)
                            AddResultToDataTable(_dataTables[IsolationsTable], result);
                        //the results of that isolation
                        if (result is MB_IsolationResult)
                            AddResultToDataTable(_dataTables[IsolationResultsTable], result);

                        if (result is MB_NoIsolations)
                            AddResultToDataTable(_dataTables[NoIsolationsTable], result);
                        if (result is MB_Tests)
                            AddResultToDataTable(_dataTables[TestsTable], result);

                        recordCount++;

                        if(recordCount%100 == 0)
                            job.OnProgress(this,new ProgressEventArgs("Load Microbiology results into memory",new ProgressMeasurement(recordCount,ProgressType.Records),sw.Elapsed ));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(
                        "Exception thrown by " + typeof (MicroBiologyFileReader).Name + " on line:" + r.LineNumber +" of file:'" + r.FileName + "' see InnerException for specifics", e);
                }

                job.OnProgress(this, new ProgressEventArgs("Load Microbiology results into memory", new ProgressMeasurement(recordCount, ProgressType.Records), sw.Elapsed));
                sw.Stop();


                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "About to bulk insert the records read from file " + fileToLoad.Name));
                //bulk insert all data from the file we just processed 
                BulkInsertAllDataTables();
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Bulk insert succesful" + fileToLoad.Name));
            }


            return ExitCodeType.Success;
        }


        private int warningsSurrendered = 0;

        void r_Warning(object sender, string message)
        {
            if(warningsSurrendered > 100)
                throw new Exception("100 Warnings encountered... maybe there is something wrong with your file? or the programmer.... best to abort anyway till you figure out the problem");

            MicroBiologyFileReader reader = (MicroBiologyFileReader) sender;
            _currentJob.OnNotify(sender,new NotifyEventArgs(ProgressEventType.Warning, "Warning encountered on line " + reader.LineNumber + " of file " + reader.FileName + " warning is:" + message));
            warningsSurrendered++;
        }

        private void BulkInsertAllDataTables()
        {
            foreach (KeyValuePair<TableInfo, DataTable> keyValuePair in _dataTables)
            {
                string targetTableName = keyValuePair.Key.GetRuntimeName(LoadStage.Mounting);

                try
                {
                    SqlConnection con = (SqlConnection) _dbInfo.Server.GetConnection();
                    con.Open();

                    SqlBulkCopy copy = new SqlBulkCopy(con);
                    copy.DestinationTableName = targetTableName;
                    copy.WriteToServer(keyValuePair.Value);
                
                    con.Close();
                    keyValuePair.Value.Clear();
                }
                catch (SqlException e)
                {
                    throw new Exception("Failed to bulk insert into table " + targetTableName,e);
                }
            }
        }

        private void AddResultToDataTable(DataTable dataTable,IMicrobiologyResultRecord result)
        {
            var dataRow = dataTable.Rows.Add();
            foreach (var property in _propertyCache[result.GetType()])
            {
                object o = property.GetValue(result);
                if (o == null)
                    dataRow[property.Name] = DBNull.Value;
                else
                {
                    if(o is string && _lengthsDictionary.ContainsKey(property))
                        if(_lengthsDictionary[property] < ((string)o).Length)
                            throw new Exception("Value '" + o + "' is too long for column " + property.Name + " when processing result of type " + result.GetType().Name );

                    dataRow[property.Name] = o;
                }
            }
        }

        private void SetupDataTables()
        {
            _dataTables.Add(LabTable,CreateDataTableFromType(typeof(MB_Lab)));
            _dataTables.Add(TestsTable, CreateDataTableFromType(typeof(MB_Tests)));
            
            _dataTables.Add(IsolationsTable, CreateDataTableFromType(typeof(MB_Isolation)));
            _dataTables.Add(IsolationResultsTable, CreateDataTableFromType(typeof(MB_IsolationResult)));

            _dataTables.Add(NoIsolationsTable, CreateDataTableFromType(typeof(MB_NoIsolations)));

            
        }

        private DataTable CreateDataTableFromType(Type t)
        {

            DataTable toReturn = new DataTable();

            if(!_propertyCache.ContainsKey(t))
                throw new Exception("Property Info Cache for type " + t.Name + " has not been initialzied yet");
         
            //now create columns in the data table for each property
            foreach (var prop in _propertyCache[t])
            {

                //if it is nullable type 
                if(prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    toReturn.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType));//give it underlying type
                else
                    toReturn.Columns.Add(prop.Name, prop.PropertyType);//else give it actual type

            }

            return toReturn;

        }

        

        public override void Check(ICheckNotifier notifier)
        {
            if (LabTable == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Required argument LabTable is missing", CheckResult.Fail, null));
            if(TestsTable == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Required argument TestsTable is missing", CheckResult.Fail, null)); 
            if(IsolationsTable == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Required argument IsolationsTable is missing", CheckResult.Fail, null));
            if (IsolationResultsTable == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Required argument IsolationResultsTable is missing", CheckResult.Fail, null));
            if(NoIsolationsTable == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Required argument NoIsolationsTable is missing", CheckResult.Fail, null));

            


            SetupPropertyBasedReflectionIntoDataTables(notifier);



        }

        private void SetupPropertyBasedReflectionIntoDataTables(ICheckNotifier notifier)
        {
            ConfirmPropertiesExist("LabTable", LabTable, typeof(MB_Lab), notifier);
            ConfirmPropertiesExist("TestsTable", TestsTable, typeof(MB_Tests), notifier);
            ConfirmPropertiesExist("IsolationsTable", IsolationsTable, typeof(MB_Isolation), notifier);
            ConfirmPropertiesExist("IsolationResultsTable", IsolationResultsTable, typeof(MB_IsolationResult), notifier);

            ConfirmPropertiesExist("NoIsolationsTable", NoIsolationsTable, typeof(MB_NoIsolations), notifier);
        }

        private void ConfirmPropertiesExist(string argumentNameForWhenMissing, TableInfo tableInfo, Type type, ICheckNotifier notifier)
        {
            if(tableInfo == null)
            {
                ComplainOrThrow("Required TableInfo argument " + argumentNameForWhenMissing + " is missing", notifier);
                return;
            }

            PropertyInfo[] properties = type.GetProperties();
            _propertyCache.Add(type, properties);//cache it so we can use it later on on a per row basis without tanking performance

            var columnInfos = tableInfo.ColumnInfos.ToArray();

            bool errors = false;
            foreach (var prop in properties)
            {
                ColumnInfo correspondingColumn = columnInfos.FirstOrDefault(c => c.GetRuntimeName().Equals(prop.Name));
                if (correspondingColumn == null)
                {
                    ComplainOrThrow("No column exists called " + prop.Name + " in TableInfo " + tableInfo.GetRuntimeName(),notifier);
                    errors = true;
                }
                else
                {
                    int maxLength = correspondingColumn.Discover(DataAccessContext.Any).DataType.GetLengthIfString();
                    if(maxLength > -1)
                        _lengthsDictionary.Add(prop,(int)maxLength);
                }
            }

            if (!errors && notifier != null)
                notifier.OnCheckPerformed(new CheckEventArgs("All columns present and correct in TableInfo " + tableInfo.GetRuntimeName() +" (when tested against underlying type " + type.Name + ")", CheckResult.Success, null));
        }

        private void ComplainOrThrow(string message,ICheckNotifier notifier)
        {
            if (notifier != null)
                notifier.OnCheckPerformed(new CheckEventArgs(message, CheckResult.Fail, null));
            else
                throw new Exception(message);
        }
    }
}
