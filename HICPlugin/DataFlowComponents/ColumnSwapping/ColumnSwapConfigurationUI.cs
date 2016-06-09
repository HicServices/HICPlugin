using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data.DataLoad;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.DataTableExtension;
using ReusableUIComponents;
using ReusableUIComponents.SqlDialogs;

namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    [Export(typeof(ICustomUI<>))]
    public partial class ColumnSwapConfigurationUI : Form, ICustomUI<ColumnSwapConfiguration>
    {
        private DataTable _preview;
        private ColumnSwapConfiguration configuration;
        
        private string[] _mappingTableColumns;
        private string[] _columnsInSource;
        
        private string _datatypeOfSubstitutionColumn;
        private string _lastSqlExecutedWhileCheckingRules;
        
        public DataTable ResultTable { get; set; }
        
        public ColumnSwapConfigurationUI()
        {
            InitializeComponent();
            RecentHistoryOfControls.GetInstance().HostControl(serverDatabaseTableSelector1.cbxServer);
            RecentHistoryOfControls.GetInstance().AddHistoryAsItemsToComboBox(serverDatabaseTableSelector1.cbxServer);
        }

        public void SetUnderlyingObjectTo(ColumnSwapConfiguration value, DataTable previewIfAvailable)
        {
            if (previewIfAvailable == null)
                throw new Exception("This interface only works when there is a preview available, at the very least can you create an empty DataTable from the TableInfos at your disposal?");
            try
            {
                _preview = previewIfAvailable;

                configuration = value??new ColumnSwapConfiguration();

                serverDatabaseTableSelector1.Server = configuration.Server;
                serverDatabaseTableSelector1.Database = configuration.Database;
                serverDatabaseTableSelector1.Table = configuration.MappingTableName;
            
                //things in the data table we are substituting for
                _columnsInSource = _preview.Columns.Cast<DataColumn>().Select(s => s.ColumnName).ToArray();
                ddColumnToSubstitute.Items.AddRange(_columnsInSource);
                ddColumnToSubstitute.SelectedItem = configuration.ColumnToPerformSubstitutionOn;

                if (configuration.SubstituteColumn != null)
                {
                    ddSubstituteFor.Items.Add(configuration.SubstituteColumn);
                    ddSubstituteFor.Text = configuration.SubstituteColumn;
                }

                cbAllow1To0Errors.Checked = configuration.Allow1ToZeroErrors;
                cbAllowMto1Errors.Checked = configuration.AllowMto1Errors;
                tbTimeout.Text = configuration.Timeout.ToString();
                cbUSeOldDateTime.Checked = configuration.UseOldDateTimes;


                //if there is a mapping table initialize the UI with that table
                if (!string.IsNullOrWhiteSpace(configuration.MappingTableName))
                    _mappingTableColumns = configuration.GetMappingTableColumns();

                foreach (SubstitutionRule rule in configuration.Rules)
                    AddRuleToTableLayoutPanel(new SubstitutionRuleUI(_columnsInSource, _mappingTableColumns, rule));
            }
            catch (Exception e)
            {
                ExceptionViewer.Show(e);
            }
        }

        public ColumnSwapConfiguration GetFinalStateOfUnderlyingObject()
        {
            return configuration;
        }

        private void btnAddRule_Click(object sender, System.EventArgs e)
        {
            AddRuleToTableLayoutPanel(new SubstitutionRuleUI(_columnsInSource, _mappingTableColumns,null));
        }

        void rule_RequestRemoval(object sender, ControlEventArgs e)
        {
            var rules = tableLayoutPanel1.Controls.Cast<SubstitutionRuleUI>().ToList();
            rules.Remove((SubstitutionRuleUI) e.Control);

            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowCount = 0;

            foreach (SubstitutionRuleUI rule in rules)
                AddRuleToTableLayoutPanel(rule);
        }

        private void AddRuleToTableLayoutPanel(SubstitutionRuleUI ruleUI)
        {
            tableLayoutPanel1.RowCount = tableLayoutPanel1.RowCount + 1;
            tableLayoutPanel1.Controls.Add(ruleUI, 0, tableLayoutPanel1.RowCount - 1);
            ruleUI.RequestRemoval += rule_RequestRemoval;

            btnCheck.Enabled = tableLayoutPanel1.Controls.Count > 0;
        }

        private void btnImportMappingTable_Click(object sender, System.EventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(serverDatabaseTableSelector1.Table))
                return;

            configuration.Server = serverDatabaseTableSelector1.Server;
            configuration.Database = serverDatabaseTableSelector1.Database;
            configuration.MappingTableName = serverDatabaseTableSelector1.Table;

            try
            {
                string previousValue = ddSubstituteFor.Text;

                _mappingTableColumns = configuration.GetMappingTableColumns();
                
                ddSubstituteFor.Items.Clear();
                ddSubstituteFor.Items.AddRange(_mappingTableColumns);

                MessageBox.Show("Identified " + _mappingTableColumns.Length + " columns");
                groupBox1.Enabled = false;
                groupBox2.Enabled = true;

                configuration.Server = serverDatabaseTableSelector1.Server;
                configuration.Database = serverDatabaseTableSelector1.Database;
                configuration.MappingTableName = serverDatabaseTableSelector1.Table;

                //reselect it if it had a value previously
                if (ddSubstituteFor.Items.Contains(previousValue))
                    ddSubstituteFor.Text = previousValue;


            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        { 
            List<SubstitutionRuleUI> priorRules = new List<SubstitutionRuleUI>();
                 
             var builder = new SqlConnectionStringBuilder()
                {
                    DataSource = serverDatabaseTableSelector1.Server,
                    InitialCatalog = "tempdb",
                    IntegratedSecurity = true
                };

            SqlConnection con = new SqlConnection(builder.ConnectionString);
            con.Open();

            try
            {
                SubstitutionRule.SubstitutionResult lastRuleValidity = null;
                string lastSqlExecuted;

                //call ApplyRule on each rule with all the details needed (mapping table, columns being substituted etc) so it can tell user about errors
                foreach (SubstitutionRuleUI rule in tableLayoutPanel1.Controls)
                {
                    lastRuleValidity = rule.ApplyRule(
                        priorRules.Select(ui => ui.SubstitutionRule).ToList(),
                        _preview.TableName,
                        serverDatabaseTableSelector1.GetTableNameFullyQualified(),
                        configuration.ColumnToPerformSubstitutionOn,
                        ddSubstituteFor.Text,
                        con,
                        configuration.Timeout,
                        out lastSqlExecuted);
                    priorRules.Add(rule);

                    _lastSqlExecutedWhileCheckingRules = lastSqlExecuted;
                }

                if (lastRuleValidity == null)
                {
                    MessageBox.Show("There were no rules to check");
                    return;
                }

                btnShowCheckingSql.Enabled = true;
                
                if (lastRuleValidity.IsExactlyOneToOne(cbAllowMto1Errors.Checked, cbAllow1To0Errors.Checked))
                {
                    if(MessageBox.Show("Rules have resulted in a 1-1 mapping, do you want to finalise mappings?","Finalise?",MessageBoxButtons.YesNo)==DialogResult.Yes)
                    {
                        groupBox4.Enabled = false;
                        groupBox5.Enabled = true;
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);

            }
            finally
            {
                con.Close();
            }
        }
        
        private void btnShowCheckingSql_Click(object sender, EventArgs e)
        {
            SQLPreviewWindow.Show("Sql that was run",
                "The following SQL was run against tempDB on server " + serverDatabaseTableSelector1.Server,
                "use tempdb" + Environment.NewLine + _lastSqlExecutedWhileCheckingRules);
        }


        private void btnUploadIntoTempDB_Click(object sender, EventArgs e)
        {
            try
            {
                DataTableHelper upload = new DataTableHelper(_preview); 
                string uploadedName = upload.CommitDataTableToTempDB(serverDatabaseTableSelector1.Result,cbUSeOldDateTime.Checked);

                MessageBox.Show("Table Uploaded into tempdb with the table name " + uploadedName);
                groupBox2.Enabled = false;
                groupBox3.Enabled = true;

            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }


        }

        private void ddSubstituteFor_SelectedIndexChanged(object sender, EventArgs e)
        {
            configuration.SubstituteColumn = ddSubstituteFor.Text;
            btnFinaliseSubstituteFor.Enabled = true;

        }

        private void btnFinaliseSubstituteFor_Click(object sender, EventArgs e)
        {
            try
            {
                var builder = serverDatabaseTableSelector1.GetBuilder();
                
                var server = new DiscoveredServer(builder);
                var table = server.GetCurrentDatabase().ExpectTable(serverDatabaseTableSelector1.Table);

                _datatypeOfSubstitutionColumn = table.DiscoverColumn(ddSubstituteFor.Text).DataType.SQLType;

                MessageBox.Show("Column " + ddSubstituteFor.Text + " will be used as a replacement for " +
                                configuration.ColumnToPerformSubstitutionOn +
                                " (which will itself be dropped after 1 to 1 mapping has been established).  The datatype replacement column (" +
                                ddSubstituteFor.Text + ") is " + _datatypeOfSubstitutionColumn);

                groupBox3.Enabled = false;
                groupBox4.Enabled = true;
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }
        }


        private void btnSaveAndClose_Click(object sender, EventArgs e)
        {
            var rules = tableLayoutPanel1.Controls.Cast<SubstitutionRuleUI>().ToList();
            
            configuration.Rules = new SubstitutionRule[rules.Count];

            for (int i = 0; i < rules.Count; i++)
            {
                configuration.Rules[i]  = new SubstitutionRule(rules[i].LeftOperand,rules[i].RightOperand);
            }

            this.Close();

        }

        private void tbTimeout_TextChanged(object sender, EventArgs e)
        {
            try
            {
                configuration.Timeout = int.Parse(tbTimeout.Text);
                tbTimeout.ForeColor = Color.Black;
            }
            catch (Exception)
            {
                tbTimeout.ForeColor = Color.Red;
            }
        }


        public void SetGenericUnderlyingObjectTo(ICustomUIDrivenClass value, DataTable previewIfAvailable)
        {
            SetUnderlyingObjectTo((ColumnSwapConfiguration)value, previewIfAvailable);
        }

        public ICustomUIDrivenClass GetGenericFinalStateOfUnderlyingObject()
        {
            return GetFinalStateOfUnderlyingObject();
        }

        private void ddColumnToSubstitute_SelectedIndexChanged(object sender, EventArgs e)
        {
            configuration.ColumnToPerformSubstitutionOn = ddColumnToSubstitute.Text;
        }

        private void cbUSeOldDateTime_CheckedChanged(object sender, EventArgs e)
        {
            configuration.UseOldDateTimes = cbUSeOldDateTime.Checked;
        }

        private void cbAllowMto1Errors_CheckedChanged(object sender, EventArgs e)
        {
            configuration.AllowMto1Errors = cbAllowMto1Errors.Checked;
        }

        private void cbAllow1To0Errors_CheckedChanged(object sender, EventArgs e)
        {

            if (cbAllow1To0Errors.Checked)
            {
                DialogResult dr = MessageBox.Show(
                    "You are about to allow the loss of study identifiers by allowing 1 to 0 mapping errors (this is where an identifier has NO MAPPING in the mapping table).  If you choose this option you will end up with NULL identifiers in your pipeline","Allow data loss?",MessageBoxButtons.YesNoCancel);

                if (dr != DialogResult.Yes)
                {
                    cbAllowMto1Errors.Checked = false;
                    return;
                }
            }
            configuration.Allow1ToZeroErrors = cbAllow1To0Errors.Checked;


            
        }
    }
}
