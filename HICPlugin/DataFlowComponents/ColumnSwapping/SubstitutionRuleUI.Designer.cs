namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    partial class SubstitutionRuleUI
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnDeleteRule = new System.Windows.Forms.Button();
            this.cbxSourceColumns = new System.Windows.Forms.ComboBox();
            this.cbxDestinationColumns = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblOneToManyErrors = new System.Windows.Forms.Label();
            this.lblManyToOneErrors = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblOneToOneSuccesses = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblOneToZeroErrors = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnDeleteRule
            // 
            this.btnDeleteRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteRule.Location = new System.Drawing.Point(1196, 9);
            this.btnDeleteRule.Name = "btnDeleteRule";
            this.btnDeleteRule.Size = new System.Drawing.Size(75, 22);
            this.btnDeleteRule.TabIndex = 0;
            this.btnDeleteRule.Text = "Delete Rule";
            this.btnDeleteRule.UseVisualStyleBackColor = true;
            this.btnDeleteRule.Click += new System.EventHandler(this.btnDeleteRule_Click);
            // 
            // cbxSourceColumns
            // 
            this.cbxSourceColumns.FormattingEnabled = true;
            this.cbxSourceColumns.Location = new System.Drawing.Point(11, 22);
            this.cbxSourceColumns.Name = "cbxSourceColumns";
            this.cbxSourceColumns.Size = new System.Drawing.Size(306, 21);
            this.cbxSourceColumns.TabIndex = 1;
            this.cbxSourceColumns.SelectedIndexChanged += new System.EventHandler(this.cbxSourceColumns_SelectedIndexChanged);
            this.cbxSourceColumns.TextChanged += new System.EventHandler(this.cbxSourceColumns_TextChanged);
            // 
            // cbxDestinationColumns
            // 
            this.cbxDestinationColumns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxDestinationColumns.FormattingEnabled = true;
            this.cbxDestinationColumns.Location = new System.Drawing.Point(342, 22);
            this.cbxDestinationColumns.Name = "cbxDestinationColumns";
            this.cbxDestinationColumns.Size = new System.Drawing.Size(437, 21);
            this.cbxDestinationColumns.TabIndex = 1;
            this.cbxDestinationColumns.SelectedIndexChanged += new System.EventHandler(this.cbxDestinationColumns_SelectedIndexChanged);
            this.cbxDestinationColumns.TextChanged += new System.EventHandler(this.cbxDestinationColumns_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(323, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(13, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "=";
            // 
            // lblOneToManyErrors
            // 
            this.lblOneToManyErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOneToManyErrors.BackColor = System.Drawing.Color.Red;
            this.lblOneToManyErrors.ForeColor = System.Drawing.Color.Gold;
            this.lblOneToManyErrors.Location = new System.Drawing.Point(1097, 8);
            this.lblOneToManyErrors.Name = "lblOneToManyErrors";
            this.lblOneToManyErrors.Size = new System.Drawing.Size(28, 23);
            this.lblOneToManyErrors.TabIndex = 3;
            this.lblOneToManyErrors.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblManyToOneErrors
            // 
            this.lblManyToOneErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblManyToOneErrors.BackColor = System.Drawing.Color.Red;
            this.lblManyToOneErrors.ForeColor = System.Drawing.Color.Gold;
            this.lblManyToOneErrors.Location = new System.Drawing.Point(1155, 7);
            this.lblManyToOneErrors.Name = "lblManyToOneErrors";
            this.lblManyToOneErrors.Size = new System.Drawing.Size(28, 23);
            this.lblManyToOneErrors.TabIndex = 3;
            this.lblManyToOneErrors.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1085, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "1-M errors";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1140, 33);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "M-1 errors";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1001, 33);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "1-1 successes";
            // 
            // lblOneToOneSuccesses
            // 
            this.lblOneToOneSuccesses.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOneToOneSuccesses.BackColor = System.Drawing.Color.LawnGreen;
            this.lblOneToOneSuccesses.Location = new System.Drawing.Point(1026, 8);
            this.lblOneToOneSuccesses.Name = "lblOneToOneSuccesses";
            this.lblOneToOneSuccesses.Size = new System.Drawing.Size(28, 23);
            this.lblOneToOneSuccesses.TabIndex = 3;
            this.lblOneToOneSuccesses.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(0, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(167, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Column In Source (or Fixed Value)";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(339, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(204, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "Column In Mapping Table (or Fixed Value)";
            // 
            // lblOneToZeroErrors
            // 
            this.lblOneToZeroErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOneToZeroErrors.BackColor = System.Drawing.Color.Red;
            this.lblOneToZeroErrors.ForeColor = System.Drawing.Color.Gold;
            this.lblOneToZeroErrors.Location = new System.Drawing.Point(953, 8);
            this.lblOneToZeroErrors.Name = "lblOneToZeroErrors";
            this.lblOneToZeroErrors.Size = new System.Drawing.Size(28, 23);
            this.lblOneToZeroErrors.TabIndex = 3;
            this.lblOneToZeroErrors.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(941, 34);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(51, 13);
            this.label9.TabIndex = 4;
            this.label9.Text = "1-0 errors";
            // 
            // SubstitutionRuleUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblManyToOneErrors);
            this.Controls.Add(this.lblOneToOneSuccesses);
            this.Controls.Add(this.lblOneToZeroErrors);
            this.Controls.Add(this.lblOneToManyErrors);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbxDestinationColumns);
            this.Controls.Add(this.cbxSourceColumns);
            this.Controls.Add(this.btnDeleteRule);
            this.Name = "SubstitutionRuleUI";
            this.Size = new System.Drawing.Size(1274, 46);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnDeleteRule;
        private System.Windows.Forms.ComboBox cbxSourceColumns;
        private System.Windows.Forms.ComboBox cbxDestinationColumns;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblOneToManyErrors;
        private System.Windows.Forms.Label lblManyToOneErrors;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblOneToOneSuccesses;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblOneToZeroErrors;
        private System.Windows.Forms.Label label9;
    }
}
