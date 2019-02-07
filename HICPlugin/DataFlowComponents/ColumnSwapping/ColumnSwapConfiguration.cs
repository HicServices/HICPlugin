using System;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CatalogueLibrary.Data.DataLoad;
using Fansi.Implementations.MicrosoftSQL;
using ReusableLibraryCode.Checks;
using FAnsi.Discovery;

namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    [Export(typeof(ICustomUIDrivenClass))]
    [Export(typeof(ICheckable))]
    public class ColumnSwapConfiguration:ICustomUIDrivenClass, ICheckable
    {
        private string _mappingTableName;

        public string Server { get; set; }
        public string Database { get; set; }

        public string MappingTableName
        {
            get { return _mappingTableName; }
            set
            {
                _mappingTableName = new MicrosoftQuerySyntaxHelper().EnsureFullyQualified(Database,null, value); 
            }
        }

        /// <summary>
        /// The input column that will be replaced
        /// </summary>
        public string ColumnToPerformSubstitutionOn { get; set; }

        /// <summary>
        /// The mapping column that replaces ColumnToPerformSubstitutionOn in the output
        /// </summary>
        public string SubstituteColumn { get; set; }
        public SubstitutionRule[] Rules { get; set; }
        public bool UseOldDateTimes { get; set; }
        public int Timeout { get; set; }

        public ColumnSwapConfiguration()
        {
            Rules = new SubstitutionRule[0];
        }

        #region Serialization
        public void RestoreStateFrom(string value)
        {
            //if the string value is empty then we don't have to do anything, we are already setup as blank
            if(string.IsNullOrWhiteSpace(value))
                return;

            XmlSerializer deserializer = new XmlSerializer(typeof(ColumnSwapConfiguration));
            var deserialized = (ColumnSwapConfiguration)deserializer.Deserialize(new StringReader(value));

            this.Server = deserialized.Server;
            this.Database = deserialized.Database;
            this.MappingTableName = deserialized.MappingTableName;
            this.ColumnToPerformSubstitutionOn = deserialized.ColumnToPerformSubstitutionOn;
            this.SubstituteColumn = deserialized.SubstituteColumn;
            this.UseOldDateTimes = deserialized.UseOldDateTimes;
            this.Timeout = deserialized.Timeout;
            AllowMto1Errors = deserialized.AllowMto1Errors;
            Allow1ToZeroErrors = deserialized.Allow1ToZeroErrors;

            if (deserialized.Rules != null)
            {
                this.Rules = new SubstitutionRule[deserialized.Rules.Length];

                for (int i = 0; i < deserialized.Rules.Length; i++)
                    this.Rules[i] = new SubstitutionRule(deserialized.Rules[i].LeftOperand,
                        deserialized.Rules[i].RightOperand);
            }
            else
                Rules = new SubstitutionRule[0];
        }

        public string SaveStateToString()
        {

            var sb = new StringBuilder();

            XmlSerializer serializer = new XmlSerializer(typeof(ColumnSwapConfiguration));

            using (var sw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
                serializer.Serialize(sw, this);

            return sb.ToString();
        }
        #endregion

        
        public bool AllowMto1Errors { get; set; }
        public bool Allow1ToZeroErrors { get; set; }

        public void Check(ICheckNotifier notifier)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Server is " + (Server ?? "null"),string.IsNullOrWhiteSpace(Server) ? CheckResult.Fail : CheckResult.Success, null));
            notifier.OnCheckPerformed(new CheckEventArgs("Database is " + (Database ?? "null"), string.IsNullOrWhiteSpace(Database) ? CheckResult.Fail : CheckResult.Success, null));
            notifier.OnCheckPerformed(new CheckEventArgs("MappingTableName is " + (MappingTableName ?? "null"), string.IsNullOrWhiteSpace(MappingTableName) ? CheckResult.Fail : CheckResult.Success, null));
            notifier.OnCheckPerformed(new CheckEventArgs("ColumnToPerformSubstitutionOn is " + (ColumnToPerformSubstitutionOn ?? "null"), string.IsNullOrWhiteSpace(ColumnToPerformSubstitutionOn) ? CheckResult.Fail : CheckResult.Success, null));
            notifier.OnCheckPerformed(new CheckEventArgs("SubstituteColumn is " + (SubstituteColumn ?? "null"), string.IsNullOrWhiteSpace(SubstituteColumn) ? CheckResult.Fail : CheckResult.Success, null));
            notifier.OnCheckPerformed(new CheckEventArgs("There are " + (Rules!= null? Rules.Length:0) + " rules configured", Rules == null || Rules.Length ==0 ? CheckResult.Fail : CheckResult.Success, null));

            if(Rules != null)
                try
                {
                    foreach ( var dodgyRule in Rules.Where(r=>string.IsNullOrWhiteSpace(r.LeftOperand) || string.IsNullOrWhiteSpace(r.RightOperand)))
                        notifier.OnCheckPerformed(new CheckEventArgs("Found rule with missing(blank) Left or Right Operand : (" + dodgyRule.LeftOperand + "|" + dodgyRule.RightOperand + ")", CheckResult.Fail, null));
                }
                catch (NullReferenceException e)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("Rules array contains null elements!", CheckResult.Fail, e));
                }

        }

        public string[] GetMappingTableColumns()
        {
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = Server,
                InitialCatalog = Database,
                IntegratedSecurity = true
            };

            var mappingTable = new DiscoveredServer(builder).ExpectDatabase(Database).ExpectTable(MappingTableName);

            if(!mappingTable.Exists())
                throw new Exception("Mapping table does not exist on server");


            return mappingTable.DiscoverColumns().Select(c => c.GetRuntimeName()).ToArray();
        }
    }
}
