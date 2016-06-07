using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine;
using DataLoadEngine.Mutilators;
using HIC.Demography;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace LoadModules.Specific.HIC
{
    
    public abstract class CHIPopulator : IPluginMutilateDataTables
    {
        private const string ChiServiceUrl = "https://hic.tayside.scot.nhs.uk/hicdemographylookupservice/api/CHILookup";
        //private const string ChiServiceUrl = "http://localhost:63804/api/CHILookup";

        [DemandsInitialization("The table containing demographical information and a CHI column that requires to be populated")] 
        public TableInfo TargetTable { get; set; }

        protected abstract IHasRuntimeName GetSurname { get; }
        protected abstract IHasRuntimeName GetForename { get; }
        protected abstract IHasRuntimeName GetDateOfBirth { get; }
        protected abstract IHasRuntimeName GetSex { get; }

        protected abstract IHasRuntimeName GetAddressLine1 { get; }
        protected abstract IHasRuntimeName GetAddressLine2 { get; }
        protected abstract IHasRuntimeName GetAddressLine3 { get; }
        protected abstract IHasRuntimeName GetAddressLine4 { get; }
        protected abstract IHasRuntimeName GetPostcode { get; }

        protected abstract IHasRuntimeName GetOtherAddressLine1 { get; }
        protected abstract IHasRuntimeName GetOtherAddressLine2 { get; }
        protected abstract IHasRuntimeName GetOtherAddressLine3 { get; }
        protected abstract IHasRuntimeName GetOtherAddressLine4 { get; }
        protected abstract IHasRuntimeName GetOtherPostcode { get; }

        
        public bool DisposeImmediately { get; set; }
        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
        }

        //populated during initialization
        private DiscoveredDatabase _dbInfo;
        
        private string[] _pks; //used to update PersonID and CHI on a per row basis
        private LoadStage _loadStage;



        public ProcessExitCode Mutilate(IDataLoadEventListener job)
        {

            _dbInfo.Server.EnableAsync();
            DiscoveredTable runtimeTable = _dbInfo.ExpectTable(_runtimeTableName);
            
            if(!runtimeTable.Exists())
                throw new Exception("Could not find table " + _runtimeTableName);

            bool mustCreatePersonIDColumn = runtimeTable.DiscoverColumns().Any(c=>c.GetRuntimeName().Equals("hic_" + CHIJob.PersonIDColumnName));

            SqlConnection con = (SqlConnection) _dbInfo.Server.GetConnection();
            con.Open();

            if (mustCreatePersonIDColumn)
            {
                SqlCommand alter = new SqlCommand("ALTER TABLE " + _runtimeTableName + " ADD hic_" + CHIJob.PersonIDColumnName + " int null" ,con);
                alter.ExecuteNonQuery();
            }

            //we require 2 connections so we can dispatch CHI lookups on one and updates with results on the other
            SqlConnection updaterConnection = (SqlConnection) _dbInfo.Server.GetConnection();
            updaterConnection.Open();
            
            int numberOfComplaints = 0;

            

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                Stopwatch clock = Stopwatch.StartNew();

                string sql = "";



                sql += @"Select CHI,";

                sql += _pks.Aggregate("",(s,n)=>s + n + ",");

                if (_forename != null)
                    sql += _forename + ",";
                if (_surname != null)
                    sql += _surname + ",";
                if (_dateOfBirth != null)
                    sql += _dateOfBirth + ",";
                if (_sex != null)
                    sql += _sex + ",";
                
                if (_postcode != null)
                    sql += _postcode + ",";
                if (_addressLine1 != null)
                    sql += _addressLine1 + ",";
                if (_addressLine2 != null)
                    sql += _addressLine2 + ",";
                if (_addressLine3 != null)
                    sql += _addressLine3 + ",";
                if (_addressLine4 != null)
                    sql += _addressLine4 + ",";

                if (_otherPostcode != null)
                    sql += _otherPostcode + ",";
                if (_otherAddressLine1 != null)
                    sql += _otherAddressLine1 + ",";
                if (_otherAddressLine2 != null)
                    sql += _otherAddressLine2 + ",";
                if (_otherAddressLine3 != null)
                    sql += _otherAddressLine3 + ",";
                if (_otherAddressLine4 != null)
                    sql += _otherAddressLine4 + ",";

                sql = sql.TrimEnd(',');

                sql += " FROM " + TargetTable.GetRuntimeName(_loadStage);

                //list of things to send to service
                SqlCommand cmd = new SqlCommand(sql,con);
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Query to fetch demographics data for CHI service is:" + cmd.CommandText));


                var r = cmd.ExecuteReader();

                int doneSoFar = 0;

                using (var client = new HttpClient())
                {
                    while (r.Read())
                    {
                        if (r["CHI"] == DBNull.Value || string.IsNullOrWhiteSpace(r["CHI"] as string))
                        {
                            var chijob = new CHIJob()
                            {
                                TargetServerName = _targetServerName,
                                TableName = _finalTableName
                            };

                            if (_forename != null)
                                chijob.Forename = r[_forename] as string;
                            if (_surname != null)
                                chijob.Surname = r[_surname] as string;
                            
                            if (_dateOfBirth != null)
                                if (r[_dateOfBirth] != DBNull.Value)
                                    chijob.DateOfBirth = Convert.ToDateTime(r[_dateOfBirth]);
                                else
                                    chijob.DateOfBirth = null;

                            if (_sex != null)
                                chijob.Sex = r[_sex] as string;

                            if (_postcode != null)
                                chijob.Postcode = r[_postcode] as string;
                            if (_addressLine1 != null)
                                chijob.AddressLine1 = r[_addressLine1] as string;
                            if (_addressLine2 != null)
                                chijob.AddressLine2 = r[_addressLine2] as string;
                            if (_addressLine3 != null)
                                chijob.AddressLine3 = r[_addressLine3] as string;
                            if (_addressLine4 != null)
                                chijob.AddressLine4 = r[_addressLine4] as string;

                            if (_otherPostcode != null)
                                chijob.OtherPostcode = r[_otherPostcode] as string;
                            if (_otherAddressLine1 != null)
                                chijob.OtherAddressLine1 = r[_otherAddressLine1] as string;
                            if (_otherAddressLine2 != null)
                                chijob.OtherAddressLine2 = r[_otherAddressLine2] as string;
                            if (_otherAddressLine3 != null)
                                chijob.OtherAddressLine3 = r[_otherAddressLine3] as string;
                            if (_otherAddressLine4 != null)
                                chijob.OtherAddressLine4 = r[_otherAddressLine4] as string;


                            CHIJobValidationResult validationResult = chijob.Validate();

                            if (validationResult.Result == ValidationCategory.InsufficientData)
                                continue;

                            var response = client.PostAsJsonAsync(ChiServiceUrl, chijob).Result.Content;
                            DemographyLookupResponse result = response.ReadAsAsync<DemographyLookupResponse>().Result;


                            if (numberOfComplaints ==10)
                                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "We have notified you of 10 errors so stopping notifying you about errors with CHI web service", result.Exception));


                            if (result.Exception != null)
                            {
                                numberOfComplaints++;

                                if (numberOfComplaints < 10)
                                    job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "CHI WebService returned an Exception",result.Exception));

                                continue;
                            }
                            ProcessResult(r, result, updaterConnection);
                        }

                        doneSoFar++;
                        job.OnProgress(this, new ProgressEventArgs("Read demography records for AutoCHI service", new ProgressMeasurement(doneSoFar, ProgressType.Records), clock.Elapsed));
                    } 
                }
            } finally 
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                con.Close();

                updaterConnection.Close();
                     
            }
            return ProcessExitCode.Success;
        }

        private void ProcessResult(SqlDataReader r, DemographyLookupResponse result,SqlConnection updaterConnection)
        {
            SqlCommand updateCommand = new SqlCommand("", updaterConnection);

            string updateSQL = "";
            Debug.Assert(result.PersonID != null, "result.PersonID != null");

            updateSQL += "UPDATE " + _runtimeTableName + " SET hic_" + CHIJob.PersonIDColumnName + " = " +((int) result.PersonID) + ", ";

            if(string.IsNullOrWhiteSpace(result.CHI))
                updateSQL += "CHI = null ";
            else
                updateSQL += "CHI = '"+result.CHI+"' ";

            updateSQL += " Where ";

            foreach (string pk in _pks)
            {
                if(r[pk] == DBNull.Value)
                    throw new Exception("Null primary key value found for column (" + string.Join(",",_pks) + "), prevents CHIing");

                updateSQL += pk + " = " + "@" +pk + " and ";

                updateCommand.Parameters.AddWithValue("@" + pk, r[pk]);
            }

            //trim off last and
            updateSQL = updateSQL.Substring(0, updateSQL.Length - " and ".Length);


            updateCommand.CommandText = updateSQL;
            int affectedRows = updateCommand.ExecuteNonQuery();

            //could be exact duplication of primary key e.g. if we are in RAW but should definetly be some affected rows!
            if(affectedRows == 0)
                throw new Exception("Zero rows affected when issuing UPDATE (to record PersonID/CHI), command was " + updateSQL);
        }


        string _forename = null;
        string _surname = null;
        private string _dateOfBirth;
        private string _postcode;
        private string _sex;
        private string _addressLine1;
        private string _addressLine2;
        private string _addressLine3;
        private string _addressLine4;
        
        private string _targetServerName;
        private string _finalTableName;
        private string _runtimeTableName;

        public void Initialize(DiscoveredDatabase dbInfo, LoadStage loadStage)
        {
            _dbInfo = dbInfo;
            _loadStage = loadStage;


            var table = dbInfo.ExpectTable(TargetTable.GetRuntimeName(_loadStage));

            if(!table.Exists())
                throw new Exception("Table " + table.GetFullyQualifiedName() + " does not exist");

            string[] availableColumns = table.DiscoverColumns().Select(c => c.GetRuntimeName()).ToArray(); 

            _forename = GetForename == null?null: GetForename.GetRuntimeName();
            _surname = GetSurname == null ? null : GetSurname.GetRuntimeName();
            _dateOfBirth = GetDateOfBirth == null ? null : GetDateOfBirth.GetRuntimeName();
            _sex = GetSex == null ? null : GetSex.GetRuntimeName();

            _postcode = GetPostcode == null ? null : GetPostcode.GetRuntimeName();
            _addressLine1 = GetAddressLine1 == null ? null : GetAddressLine1.GetRuntimeName();
            _addressLine2 = GetAddressLine2 == null ? null : GetAddressLine2.GetRuntimeName();
            _addressLine3 = GetAddressLine3 == null ? null : GetAddressLine3.GetRuntimeName();
            _addressLine4 = GetAddressLine4 == null ? null : GetAddressLine4.GetRuntimeName();


            _otherPostcode = GetOtherPostcode == null ? null : GetOtherPostcode.GetRuntimeName();
            _otherAddressLine1 = GetOtherAddressLine1 == null ? null : GetOtherAddressLine1.GetRuntimeName();
            _otherAddressLine2 = GetOtherAddressLine2 == null ? null : GetOtherAddressLine2.GetRuntimeName();
            _otherAddressLine3 = GetOtherAddressLine3 == null ? null : GetOtherAddressLine3.GetRuntimeName();
            _otherAddressLine4 = GetOtherAddressLine4 == null ? null : GetOtherAddressLine4.GetRuntimeName();


            CheckColumnIsInCollection(_forename, availableColumns);
            CheckColumnIsInCollection(_surname, availableColumns);
            CheckColumnIsInCollection(_dateOfBirth, availableColumns);
            CheckColumnIsInCollection(_sex, availableColumns);

            CheckColumnIsInCollection(_postcode, availableColumns);
            CheckColumnIsInCollection(_addressLine1, availableColumns);
            CheckColumnIsInCollection(_addressLine2, availableColumns);
            CheckColumnIsInCollection(_addressLine3, availableColumns);
            CheckColumnIsInCollection(_addressLine4, availableColumns);

            CheckColumnIsInCollection(_otherPostcode, availableColumns);
            CheckColumnIsInCollection(_otherAddressLine1, availableColumns);
            CheckColumnIsInCollection(_otherAddressLine2, availableColumns);
            CheckColumnIsInCollection(_otherAddressLine3, availableColumns);
            CheckColumnIsInCollection(_otherAddressLine4, availableColumns);
            
            _targetServerName = TargetTable.Server;
            
            
            _finalTableName = TargetTable.Name;
            _runtimeTableName = TargetTable.GetRuntimeName(_loadStage);

            _pks = ColumnInfo.GetAllColumnInfosForParent(TargetTable).Where(c => c.IsPrimaryKey).Select(p => p.GetRuntimeName(loadStage)).ToArray();
        }

        private void CheckColumnIsInCollection(string toFind, string[] availableColumns)
        {
            if (toFind != null && !availableColumns.Contains(toFind))
                throw new KeyNotFoundException("Could not find column " + toFind + "in dataset, either the ColumnInfo arguments are incorrect about column names or they do not have values");
        }

        public string DatabaseServer { get; private set; }
        public string DatabaseName { get; private set; }

        

        public void Check(ICheckNotifier notifier)
        {
            var chijob = new CHIJob()
            {
                TargetServerName = "TEST",
                TableName = "TEST.dbo.TEST",
                Forename = "Test",
                Surname = "Test"
            };
            HttpContent response = null;
            try
            {
                //see if we can reach the destination server
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                using (var client = new HttpClient())
                {
                    response = client.PostAsJsonAsync(ChiServiceUrl, chijob).Result.Content;

                    var returnedValue = response.ReadAsAsync<DemographyLookupResponse>().Result;

                    if (returnedValue.Exception != null)
                        notifier.OnCheckPerformed(new CheckEventArgs("CHI web service returned an Exception instead of a PersonID/CHI", CheckResult.Fail,
                            returnedValue.Exception));
                    else
                        notifier.OnCheckPerformed(new CheckEventArgs("Succesfully connected to web service for CHIing", CheckResult.Success,
                        null));
                }
            }
            catch (Exception e)
            {
                //response failed try reading as string
                if (response != null)
                {
                    try
                    {
                        StreamReader sr = new StreamReader(response.ReadAsStreamAsync().Result);
                        notifier.OnCheckPerformed(new CheckEventArgs(
                            "CHI web service returned the following text, which could not be converted into a DemographyLookupResponse:" +
                            Environment.NewLine + sr.ReadToEnd(), CheckResult.Fail, null));
                    }
                    catch (Exception exception)
                    {
                        notifier.OnCheckPerformed(new CheckEventArgs(
                         "CHI web service returned something that could not be converted into a DemographyLookupResponse or a string.", CheckResult.Fail, exception));
                    }
                }

                notifier.OnCheckPerformed(new CheckEventArgs(
                    "Problem accessing CHI web service - or some other problem, check Exception for details",
                    CheckResult.Fail, e));
            }
            finally
            {
                //unregister
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }

            var allColumnInfos = ColumnInfo.GetAllColumnInfosForParent(TargetTable).ToArray();

            ColumnInfo personID = allColumnInfos.SingleOrDefault(c => c.GetRuntimeName().Equals("hic_" + CHIJob.PersonIDColumnName));
            ColumnInfo chiColumn = allColumnInfos.SingleOrDefault(c => c.GetRuntimeName().Equals("CHI"));


            //make sure there is a PersonID column in the table
            if (personID == null)
                notifier.OnCheckPerformed(new CheckEventArgs("Column " + "hic_" + CHIJob.PersonIDColumnName + " does not exist in TargetTable " + TargetTable + " (this must exist for scheduling unknown CHI jobs for Data Entry)", CheckResult.Fail, null));

            if (chiColumn == null)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("There is no Column called 'CHI' so cannot populate CHIs ", CheckResult.Fail,null));
                return;
            }

            var primaryKeyColumns = ColumnInfo.GetAllColumnInfosForParent(TargetTable).Where(c => c.IsPrimaryKey).ToArray();

            if (primaryKeyColumns.Any(p => p.ID == chiColumn.ID))
                throw new Exception("CHI cannot be part of the primary key when demography based linkage is being employed by CHIPopulator - since CHI could be null quite often");

            if (personID == null)
                return;

            if (primaryKeyColumns.Any(p => p.ID == personID.ID))
                throw new Exception("PersonID cannot be part of the primary key when demography based linkage is being employed because it is assigned by the CHI webservice and we need to update the data table (based on the primary key) with the number that comes back from the webservice - Chicken And Egg");


        }

        private int stackOverFlowPreventer = 0;
        private string _otherPostcode;
        private string _otherAddressLine1;
        private string _otherAddressLine2;
        private string _otherAddressLine3;
        private string _otherAddressLine4;


        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (stackOverFlowPreventer == 5)
                return null;

            stackOverFlowPreventer++;

            //can end up in stack overflow - this explodes but instead calls the containing method itself... srs?
            Assembly a = Assembly.Load(args.Name);

            stackOverFlowPreventer = 0;

            if (a != null)
                return a;
            else
                return null;
        }
    }
}

