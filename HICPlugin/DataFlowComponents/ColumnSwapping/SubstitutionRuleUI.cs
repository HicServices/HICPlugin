using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.DataHelper;
using ReusableUIComponents;

namespace LoadModules.Specific.HIC.DataFlowComponents.ColumnSwapping
{
    public partial class SubstitutionRuleUI : UserControl
    {
        public event ControlEventHandler RequestRemoval;

        public SubstitutionRule SubstitutionRule { get; set; }
        
        public string LeftOperand {
            get { return cbxSourceColumns.Text; }
        }
        public string RightOperand
        {
            get { return cbxDestinationColumns.Text; }
        }

        private bool bLoading = false;
        public SubstitutionRuleUI(string[] sourceTableColumns, string[] mappingTableColumns,SubstitutionRule rule)
        {
            bLoading = true;
            InitializeComponent();

            cbxSourceColumns.Items.AddRange(sourceTableColumns.Select(s => QuerySyntaxHelper.EnsureValueIsWrapped(s)).ToArray());
            cbxDestinationColumns.Items.AddRange(mappingTableColumns.Select(s=> QuerySyntaxHelper.EnsureValueIsWrapped(s)).ToArray());

            BetterToolTip toolTip = new BetterToolTip(this);

            toolTip.SetToolTip(lblManyToOneErrors, ToolTips.MTo1, Images.MTo1);
            toolTip.SetToolTip(lblOneToZeroErrors, ToolTips._1To0, Images._1To0);
            toolTip.SetToolTip(lblOneToManyErrors, ToolTips._1ToM, Images._1ToM);

            SubstitutionRule = rule ?? new SubstitutionRule();
            
            cbxSourceColumns.Text = SubstitutionRule.LeftOperand;
            cbxDestinationColumns.Text = SubstitutionRule.RightOperand;
            bLoading = false;
        }

        private void btnDeleteRule_Click(object sender, EventArgs e)
        {
            RequestRemoval(this, new ControlEventArgs(this));
        }


        public SubstitutionRule.SubstitutionResult ApplyRule(List<SubstitutionRule> priorRules, string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable, SqlConnection conToSourceTable, int timeout, out string sql)
        {

            lblOneToZeroErrors.Text = "0";
            lblOneToOneSuccesses.Text = "0";
            lblOneToManyErrors.Text = "0";
            lblManyToOneErrors.Text = "Unk";

            var result = SubstitutionRule.CheckRule(priorRules, sqlOriginTable, sqlMappingTable, substituteInSourceColumn,substituteForInMappingTable, conToSourceTable, timeout, out sql);

            lblOneToZeroErrors.Text = result.OneToZeroErrors.ToString();
            lblOneToOneSuccesses.Text = result.OneToOneSucceses.ToString();
            lblOneToManyErrors.Text = result.OneToManyErrors.ToString();
            lblManyToOneErrors.Text = result.ManyToOneErrors.ToString();
            
            return result;

        }

        private void cbxSourceColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUnderlyingRuleObjectOperands();
        }

        private void cbxSourceColumns_TextChanged(object sender, EventArgs e)
        {
            UpdateUnderlyingRuleObjectOperands();
        }

        private void cbxDestinationColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUnderlyingRuleObjectOperands();
        }

        private void cbxDestinationColumns_TextChanged(object sender, EventArgs e)
        {
            UpdateUnderlyingRuleObjectOperands();
        }

        private void UpdateUnderlyingRuleObjectOperands()
        {
            if(bLoading)
                return;

            SubstitutionRule.LeftOperand = cbxSourceColumns.Text;
            SubstitutionRule.RightOperand = cbxDestinationColumns.Text;
        }
    }
}
