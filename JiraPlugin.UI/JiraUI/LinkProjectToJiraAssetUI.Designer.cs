namespace JiraPlugin.UI.JiraUI;

partial class LinkProjectToJiraAssetUI
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
        cbJiraConfiguration = new ComboBox();
        label1 = new Label();
        label2 = new Label();
        lblError = new Label();
        cbAssets = new ComboBox();
        label3 = new Label();
        cbProject = new ComboBox();
        button1 = new Button();
        SuspendLayout();
        // 
        // cbJiraConfiguration
        // 
        cbJiraConfiguration.Enabled = false;
        cbJiraConfiguration.FormattingEnabled = true;
        cbJiraConfiguration.Location = new Point(138, 51);
        cbJiraConfiguration.Name = "cbJiraConfiguration";
        cbJiraConfiguration.Size = new Size(242, 23);
        cbJiraConfiguration.TabIndex = 0;
        cbJiraConfiguration.SelectedIndexChanged += cbJiraConfiguration_SelectedIndexChanged;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(31, 54);
        label1.Name = "label1";
        label1.Size = new Size(101, 15);
        label1.TabIndex = 1;
        label1.Text = "Jira Configuration";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(77, 107);
        label2.Name = "label2";
        label2.Size = new Size(55, 15);
        label2.TabIndex = 2;
        label2.Text = "Jira Asset";
        // 
        // lblError
        // 
        lblError.AutoSize = true;
        lblError.Font = new Font("Segoe UI", 12F);
        lblError.ForeColor = Color.Red;
        lblError.Location = new Point(31, 177);
        lblError.Name = "lblError";
        lblError.Size = new Size(52, 21);
        lblError.TabIndex = 4;
        lblError.Text = "label3";
        lblError.Visible = false;
        // 
        // cbAssets
        // 
        cbAssets.Enabled = false;
        cbAssets.FormattingEnabled = true;
        cbAssets.Location = new Point(138, 104);
        cbAssets.Name = "cbAssets";
        cbAssets.Size = new Size(242, 23);
        cbAssets.TabIndex = 5;
        cbAssets.SelectedIndexChanged += cbAssets_SelectedIndexChanged;
        cbAssets.TextChanged += cbAssets_TextChanged;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(88, 13);
        label3.Name = "label3";
        label3.Size = new Size(44, 15);
        label3.TabIndex = 6;
        label3.Text = "Project";
        // 
        // cbProject
        // 
        cbProject.Enabled = false;
        cbProject.FormattingEnabled = true;
        cbProject.Location = new Point(138, 10);
        cbProject.Name = "cbProject";
        cbProject.Size = new Size(242, 23);
        cbProject.TabIndex = 7;
        // 
        // button1
        // 
        button1.Enabled = false;
        button1.Location = new Point(249, 148);
        button1.Name = "button1";
        button1.Size = new Size(131, 23);
        button1.TabIndex = 8;
        button1.Text = "Link External Asset";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // LinkProjectToJiraAssetUI
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(button1);
        Controls.Add(cbProject);
        Controls.Add(label3);
        Controls.Add(cbAssets);
        Controls.Add(lblError);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(cbJiraConfiguration);
        Name = "LinkProjectToJiraAssetUI";
        Size = new Size(1094, 464);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ComboBox cbJiraConfiguration;
    private Label label1;
    private Label label2;
    private Label lblError;
    private ComboBox cbAssets;
    private Label label3;
    private ComboBox cbProject;
    private Button button1;
}
