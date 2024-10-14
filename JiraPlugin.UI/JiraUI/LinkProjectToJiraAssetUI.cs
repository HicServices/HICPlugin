using Rdmp.UI.TestsAndSetup.ServicePropogation;
using Rdmp.UI;
using System.ComponentModel;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Curation.Data;
using Rdmp.UI.ItemActivation;
using InterfaceToJira.RestApiClient2;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using HIC.Common.InterfaceToJira;

namespace JiraPlugin.UI.JiraUI;

public partial class LinkProjectToJiraAssetUI : LinkProjectToJiraAssetUI_Design
{
    private Project _project;
    private TicketingSystemConfiguration _ticketingSystemConfiguration;
    private JiraAPIClient _client;
    public LinkProjectToJiraAssetUI()
    {
        InitializeComponent();
    }

    public override void SetDatabaseObject(IActivateItems activator, Project databaseObject)
    {
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

    }

    public override string GetTabName() => $"Link Project to Jira Asset";

    private void cbJiraConfiguration_SelectedIndexChanged(object sender, EventArgs e)
    {
        _ticketingSystemConfiguration = (TicketingSystemConfiguration)cbJiraConfiguration.Items[cbJiraConfiguration.SelectedIndex];
        //var jiraAccount = new JiraAccount(new HIC.Common.InterfaceToJira.JiraApiConfiguration()
        //{
        //    ServerUrl = _ticketingSystemConfiguration.Url,
        //    User = _ticketingSystemConfiguration.DataAccessCredentials.Username,
        //    Password = _ticketingSystemConfiguration.DataAccessCredentials.GetDecryptedPassword(),
        //});
        //_client = new JiraAPIClient(jiraAccount);
        _client ??= new JiraAPIClient(new JiraAccount(new JiraApiConfiguration
        {
            ServerUrl = _ticketingSystemConfiguration.Url,
            User = _ticketingSystemConfiguration.DataAccessCredentials.Username,
            Password = _ticketingSystemConfiguration.DataAccessCredentials.GetDecryptedPassword(),
            ApiUrl = _ticketingSystemConfiguration.Url
        }));
        var projects = _client.GetAllProjectAssets();
        cbAssets.Items.Clear();
        cbAssets.Items.Add(projects);
        cbAssets.Enabled = true;
    }
}


//[TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<LinkProjectToJiraAssetUI_Design, UserControl>))]
//public abstract class LinkProjectToJiraAssetUI_Design : RDMPSingleDatabaseObjectControl<Project>
//{
//}