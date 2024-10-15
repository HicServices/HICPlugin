using Rdmp.UI.TestsAndSetup.ServicePropogation;
using Rdmp.UI;
using System.ComponentModel;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Curation.Data;
using Rdmp.UI.ItemActivation;
using InterfaceToJira.RestApiClient2;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using HIC.Common.InterfaceToJira;
using InterfaceToJira.RestApiClient2.JiraModel;
using Rdmp.Core.Curation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace JiraPlugin.UI.JiraUI;

public partial class LinkProjectToJiraAssetUI : LinkProjectToJiraAssetUI_Design
{
    private IActivateItems _activator;
    private Project _project;
    private TicketingSystemConfiguration _ticketingSystemConfiguration;
    private JiraAPIClient _client;
    private List<JiraAsset> _jiraProjects;
    public LinkProjectToJiraAssetUI()
    {
        InitializeComponent();
    }

    public override void SetDatabaseObject(IActivateItems activator, Project databaseObject)
    {
        _activator = activator;
        _project = databaseObject;
        var jiraAccounts = activator.RepositoryLocator.CatalogueRepository.GetAllObjectsWhere<TicketingSystemConfiguration>("Type", "JiraPlugin.JIRATicketingSystem");
        if (!jiraAccounts.Any())
        {
            //error out
            lblError.Text = "No Jira Ticketing systems found. Please create one before attempting to link to Jira assets.";
            lblError.Visible = true;
            return;
        }
        cbJiraConfiguration.Items.Clear();
        cbJiraConfiguration.Items.AddRange(jiraAccounts);
        cbJiraConfiguration.Enabled = true;

        cbProject.Text = _project.Name;

    }

    public override string GetTabName() => $"Link Project to Jira Asset";

    private void cbJiraConfiguration_SelectedIndexChanged(object sender, EventArgs e)
    {
        _ticketingSystemConfiguration = (TicketingSystemConfiguration)cbJiraConfiguration.Items[cbJiraConfiguration.SelectedIndex];
        _client ??= new JiraAPIClient(new JiraAccount(new JiraApiConfiguration
        {
            ServerUrl = _ticketingSystemConfiguration.Url,
            User = _ticketingSystemConfiguration.DataAccessCredentials.Username,
            Password = _ticketingSystemConfiguration.DataAccessCredentials.GetDecryptedPassword(),
            ApiUrl = _ticketingSystemConfiguration.Url
        }));
        _jiraProjects = _client.GetAllProjectAssets();
        cbAssets.Items.Clear();
        cbAssets.Items.AddRange(_jiraProjects.ToArray());
        cbAssets.Enabled = true;

        if(cbAssets.SelectedIndex != null)
        {
            button1.Enabled = true;
        }
    }

    private void cbAssets_TextChanged(object sender, EventArgs e)
    {
        //var txt = cbAssets.Text.ToLower();
        //var matchs = _jiraProjects.Where(p => p.ToString().ToLower().Contains(txt));
        //cbAssets.Items.Clear();
        //cbAssets.Items.AddRange(matchs.ToArray());
        //cbAssets.DroppedDown = true;
        //Cursor.Current = Cursors.Default;
        //cbAssets.Select(txt.Length, 0);
    }

    private void cbAssets_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    private void button1_Click(object sender, EventArgs e)
    {
        var selectedAsset = _jiraProjects[cbAssets.SelectedIndex];
        var asset = new ExternalAsset(_activator.RepositoryLocator.CatalogueRepository, selectedAsset.ToString(), selectedAsset.id, _ticketingSystemConfiguration.ID, _project.GetType().ToString(), _project.ID);
        asset.SaveToDatabase();
        //todo close the user control
        _activator.Show("External asset successfully linked with the project.");
        
    }
}