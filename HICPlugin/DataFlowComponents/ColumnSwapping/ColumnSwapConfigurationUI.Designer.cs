using ReusableUIComponents;

namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    partial class ColumnSwapConfigurationUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnImportMappingTable = new System.Windows.Forms.Button();
            this.serverDatabaseTableSelector1 = new ReusableUIComponents.ServerDatabaseTableSelector();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnAddRule = new System.Windows.Forms.Button();
            this.btnShowCheckingSql = new System.Windows.Forms.Button();
            this.btnCheck = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ddSubstituteFor = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbUSeOldDateTime = new System.Windows.Forms.CheckBox();
            this.btnUploadIntoTempDB = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.ddColumnToSubstitute = new System.Windows.Forms.ComboBox();
            this.btnFinaliseSubstituteFor = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.btnSaveAndClose = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbTimeout = new System.Windows.Forms.TextBox();
            this.cbAllowMto1Errors = new System.Windows.Forms.CheckBox();
            this.cbAllow1To0Errors = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnImportMappingTable);
            this.groupBox1.Controls.Add(this.serverDatabaseTableSelector1);
            this.groupBox1.Location = new System.Drawing.Point(18, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(563, 180);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "1. Select Table To Substitute From";
            // 
            // btnImportMappingTable
            // 
            this.btnImportMappingTable.Location = new System.Drawing.Point(355, 151);
            this.btnImportMappingTable.Name = "btnImportMappingTable";
            this.btnImportMappingTable.Size = new System.Drawing.Size(202, 23);
            this.btnImportMappingTable.TabIndex = 3;
            this.btnImportMappingTable.Text = "Download Mapping Table Columns";
            this.btnImportMappingTable.UseVisualStyleBackColor = true;
            this.btnImportMappingTable.Click += new System.EventHandler(this.btnImportMappingTable_Click);
            // 
            // serverDatabaseTableSelector1
            // 
            this.serverDatabaseTableSelector1.AllowTableValuedFunctionSelection = false;
            this.serverDatabaseTableSelector1.AutoSize = true;
            this.serverDatabaseTableSelector1.Database = "";
            this.serverDatabaseTableSelector1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.serverDatabaseTableSelector1.Location = new System.Drawing.Point(3, 16);
            this.serverDatabaseTableSelector1.Name = "serverDatabaseTableSelector1";
            this.serverDatabaseTableSelector1.Password = "";
            this.serverDatabaseTableSelector1.Server = "";
            this.serverDatabaseTableSelector1.Size = new System.Drawing.Size(557, 161);
            this.serverDatabaseTableSelector1.TabIndex = 0;
            this.serverDatabaseTableSelector1.Table = "";
            this.serverDatabaseTableSelector1.Username = "";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.tableLayoutPanel1);
            this.groupBox4.Controls.Add(this.btnAddRule);
            this.groupBox4.Controls.Add(this.btnShowCheckingSql);
            this.groupBox4.Controls.Add(this.btnCheck);
            this.groupBox4.Enabled = false;
            this.groupBox4.Location = new System.Drawing.Point(12, 298);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(1329, 419);
            this.groupBox4.TabIndex = 2;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "4. Specify Substitution Rules (Rules are applied to find matches using AND operat" +
    "or)";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoScroll = true;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1323F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 43);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1323, 341);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // btnAddRule
            // 
            this.btnAddRule.Location = new System.Drawing.Point(6, 19);
            this.btnAddRule.Name = "btnAddRule";
            this.btnAddRule.Size = new System.Drawing.Size(75, 23);
            this.btnAddRule.TabIndex = 3;
            this.btnAddRule.Text = "Add Rule";
            this.btnAddRule.UseVisualStyleBackColor = true;
            this.btnAddRule.Click += new System.EventHandler(this.btnAddRule_Click);
            // 
            // btnShowCheckingSql
            // 
            this.btnShowCheckingSql.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnShowCheckingSql.Enabled = false;
            this.btnShowCheckingSql.Location = new System.Drawing.Point(90, 390);
            this.btnShowCheckingSql.Name = "btnShowCheckingSql";
            this.btnShowCheckingSql.Size = new System.Drawing.Size(248, 23);
            this.btnShowCheckingSql.TabIndex = 3;
            this.btnShowCheckingSql.Text = "Show Checking SQL (For All Rules Combined)";
            this.btnShowCheckingSql.UseVisualStyleBackColor = true;
            this.btnShowCheckingSql.Click += new System.EventHandler(this.btnShowCheckingSql_Click);
            // 
            // btnCheck
            // 
            this.btnCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCheck.Enabled = false;
            this.btnCheck.Location = new System.Drawing.Point(6, 390);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(78, 23);
            this.btnCheck.TabIndex = 3;
            this.btnCheck.Text = "Check";
            this.btnCheck.UseVisualStyleBackColor = true;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Substite Column:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(476, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "For Column:";
            // 
            // ddSubstituteFor
            // 
            this.ddSubstituteFor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddSubstituteFor.FormattingEnabled = true;
            this.ddSubstituteFor.Location = new System.Drawing.Point(545, 13);
            this.ddSubstituteFor.Name = "ddSubstituteFor";
            this.ddSubstituteFor.Size = new System.Drawing.Size(506, 21);
            this.ddSubstituteFor.TabIndex = 5;
            this.ddSubstituteFor.SelectedIndexChanged += new System.EventHandler(this.ddSubstituteFor_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cbUSeOldDateTime);
            this.groupBox2.Controls.Add(this.btnUploadIntoTempDB);
            this.groupBox2.Enabled = false;
            this.groupBox2.Location = new System.Drawing.Point(15, 198);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(932, 51);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "2. Upload CSV";
            // 
            // cbUSeOldDateTime
            // 
            this.cbUSeOldDateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUSeOldDateTime.AutoSize = true;
            this.cbUSeOldDateTime.Location = new System.Drawing.Point(738, 23);
            this.cbUSeOldDateTime.Name = "cbUSeOldDateTime";
            this.cbUSeOldDateTime.Size = new System.Drawing.Size(188, 17);
            this.cbUSeOldDateTime.TabIndex = 5;
            this.cbUSeOldDateTime.Text = "Server does not support datetime2";
            this.cbUSeOldDateTime.UseVisualStyleBackColor = true;
            this.cbUSeOldDateTime.CheckedChanged += new System.EventHandler(this.cbUSeOldDateTime_CheckedChanged);
            // 
            // btnUploadIntoTempDB
            // 
            this.btnUploadIntoTempDB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUploadIntoTempDB.Location = new System.Drawing.Point(16, 19);
            this.btnUploadIntoTempDB.Name = "btnUploadIntoTempDB";
            this.btnUploadIntoTempDB.Size = new System.Drawing.Size(716, 23);
            this.btnUploadIntoTempDB.TabIndex = 4;
            this.btnUploadIntoTempDB.Text = "Bulk Insert CSV file into tempDB";
            this.btnUploadIntoTempDB.UseVisualStyleBackColor = true;
            this.btnUploadIntoTempDB.Click += new System.EventHandler(this.btnUploadIntoTempDB_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.ddColumnToSubstitute);
            this.groupBox3.Controls.Add(this.btnFinaliseSubstituteFor);
            this.groupBox3.Controls.Add(this.ddSubstituteFor);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Enabled = false;
            this.groupBox3.Location = new System.Drawing.Point(15, 256);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1109, 40);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "3. Choose Substitute Column";
            // 
            // ddColumnToSubstitute
            // 
            this.ddColumnToSubstitute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddColumnToSubstitute.FormattingEnabled = true;
            this.ddColumnToSubstitute.Location = new System.Drawing.Point(103, 13);
            this.ddColumnToSubstitute.Name = "ddColumnToSubstitute";
            this.ddColumnToSubstitute.Size = new System.Drawing.Size(367, 21);
            this.ddColumnToSubstitute.TabIndex = 7;
            this.ddColumnToSubstitute.SelectedIndexChanged += new System.EventHandler(this.ddColumnToSubstitute_SelectedIndexChanged);
            // 
            // btnFinaliseSubstituteFor
            // 
            this.btnFinaliseSubstituteFor.Enabled = false;
            this.btnFinaliseSubstituteFor.Location = new System.Drawing.Point(1057, 11);
            this.btnFinaliseSubstituteFor.Name = "btnFinaliseSubstituteFor";
            this.btnFinaliseSubstituteFor.Size = new System.Drawing.Size(39, 23);
            this.btnFinaliseSubstituteFor.TabIndex = 6;
            this.btnFinaliseSubstituteFor.Text = "Ok";
            this.btnFinaliseSubstituteFor.UseVisualStyleBackColor = true;
            this.btnFinaliseSubstituteFor.Click += new System.EventHandler(this.btnFinaliseSubstituteFor_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox5.Controls.Add(this.btnSaveAndClose);
            this.groupBox5.Enabled = false;
            this.groupBox5.Location = new System.Drawing.Point(12, 719);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(566, 51);
            this.groupBox5.TabIndex = 8;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "5. Save And Close";
            // 
            // btnSaveAndClose
            // 
            this.btnSaveAndClose.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveAndClose.Location = new System.Drawing.Point(16, 19);
            this.btnSaveAndClose.Name = "btnSaveAndClose";
            this.btnSaveAndClose.Size = new System.Drawing.Size(532, 23);
            this.btnSaveAndClose.TabIndex = 4;
            this.btnSaveAndClose.Text = "Save And Close";
            this.btnSaveAndClose.UseVisualStyleBackColor = true;
            this.btnSaveAndClose.Click += new System.EventHandler(this.btnSaveAndClose_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1155, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Timeout (in s):";
            // 
            // tbTimeout
            // 
            this.tbTimeout.Location = new System.Drawing.Point(1234, 10);
            this.tbTimeout.Name = "tbTimeout";
            this.tbTimeout.Size = new System.Drawing.Size(100, 20);
            this.tbTimeout.TabIndex = 10;
            this.tbTimeout.Text = "30";
            this.tbTimeout.TextChanged += new System.EventHandler(this.tbTimeout_TextChanged);
            // 
            // cbAllowMto1Errors
            // 
            this.cbAllowMto1Errors.AutoSize = true;
            this.cbAllowMto1Errors.Location = new System.Drawing.Point(1146, 36);
            this.cbAllowMto1Errors.Name = "cbAllowMto1Errors";
            this.cbAllowMto1Errors.Size = new System.Drawing.Size(195, 17);
            this.cbAllowMto1Errors.TabIndex = 11;
            this.cbAllowMto1Errors.Text = "Allow Data Loss From M To 1 Errors";
            this.cbAllowMto1Errors.UseVisualStyleBackColor = true;
            this.cbAllowMto1Errors.CheckedChanged += new System.EventHandler(this.cbAllowMto1Errors_CheckedChanged);
            // 
            // cbAllow1To0Errors
            // 
            this.cbAllow1To0Errors.AutoSize = true;
            this.cbAllow1To0Errors.Location = new System.Drawing.Point(1146, 59);
            this.cbAllow1To0Errors.Name = "cbAllow1To0Errors";
            this.cbAllow1To0Errors.Size = new System.Drawing.Size(188, 17);
            this.cbAllow1To0Errors.TabIndex = 12;
            this.cbAllow1To0Errors.Text = "Allow Data Loss From 1 to 0 Errors";
            this.cbAllow1To0Errors.UseVisualStyleBackColor = true;
            this.cbAllow1To0Errors.CheckedChanged += new System.EventHandler(this.cbAllow1To0Errors_CheckedChanged);
            // 
            // ColumnSwapConfigurationUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1353, 774);
            this.Controls.Add(this.cbAllow1To0Errors);
            this.Controls.Add(this.cbAllowMto1Errors);
            this.Controls.Add(this.tbTimeout);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox1);
            this.Name = "ColumnSwapConfigurationUI";
            this.Text = "SubstituteAColumnUI";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ServerDatabaseTableSelector serverDatabaseTableSelector1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnAddRule;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnImportMappingTable;
        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox ddSubstituteFor;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnUploadIntoTempDB;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button btnSaveAndClose;
        private System.Windows.Forms.Button btnFinaliseSubstituteFor;
        private System.Windows.Forms.Button btnShowCheckingSql;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbTimeout;
        private System.Windows.Forms.CheckBox cbUSeOldDateTime;
        private System.Windows.Forms.ComboBox ddColumnToSubstitute;
        private System.Windows.Forms.CheckBox cbAllowMto1Errors;
        private System.Windows.Forms.CheckBox cbAllow1To0Errors;
    }
}