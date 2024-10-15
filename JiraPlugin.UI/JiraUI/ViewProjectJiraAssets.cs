using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using HIC.Common.InterfaceToJira;
using InterfaceToJira.RestApiClient2;
using InterfaceToJira.RestApiClient2.JiraModel;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.ItemActivation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rdmp.Core.ReusableLibraryCode;

namespace JiraPlugin.UI.JiraUI
{
    public partial class ViewProjectJiraAssets : LinkProjectToJiraAssetUI_Design
    {

        private List<ExternalAsset> _externalAssets;
        private List<JiraAsset> _jiraAssets;
        private JiraAPIClient _client;
        public ViewProjectJiraAssets()
        {
            InitializeComponent();
        }


        public override void SetDatabaseObject(IActivateItems activator, Project databaseObject)
        {
            base.SetDatabaseObject(activator, databaseObject);
            _externalAssets = activator.RepositoryLocator.CatalogueRepository.GetAllObjectsWhere<ExternalAsset>("ObjectID", databaseObject.ID).Where(ea => ea.ObjectType == typeof(Project).ToString()).ToList();
            DataTable dt = new();
            dt.Columns.Add("Name");
            dt.Columns.Add("URL");
            //dt.Columns.Add("Link");
            foreach (var asset in _externalAssets)
            {
                var ticketingSystemConfiguration = activator.RepositoryLocator.CatalogueRepository.GetAllObjectsWhere<TicketingSystemConfiguration>("ID", asset.TicketingConfiguration_ID).FirstOrDefault();
                if (ticketingSystemConfiguration is null)
                {
                    throw new Exception("Unable to find ticketing system");
                }
                _client ??= new JiraAPIClient(new JiraAccount(new JiraApiConfiguration
                {
                    ServerUrl = ticketingSystemConfiguration.Url,
                    User = ticketingSystemConfiguration.DataAccessCredentials.Username,
                    Password = ticketingSystemConfiguration.DataAccessCredentials.GetDecryptedPassword(),
                    ApiUrl = ticketingSystemConfiguration.Url
                }));
                var jiraAsset = _client.GetProjectAsset(asset.ExternalAsset_ID);
                var url = $"{ticketingSystemConfiguration.Url}/jira/servicedesk/assets/object-schema/3?typeId={jiraAsset.objectType.id}&view=list&objectId={jiraAsset.id}";
                dt.Rows.Add(asset.Name, url);
            }
            dataGridView1.DataSource = dt;

            DataGridViewButtonColumn button = new DataGridViewButtonColumn();
            {
                button.Name = "View";
                button.HeaderText = "View";
                button.Text = "View";
                button.UseColumnTextForButtonValue = true; //dont forget this line
                this.dataGridView1.Columns.Add(button);
            }
            dataGridView1.CellClick += dataGridView1_CellClick;

            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["view"].Index)
            {
                string selectedValue = dataGridView1.Rows[e.RowIndex].Cells["URL"].Value.ToString();
                UsefulStuff.OpenUrl(selectedValue);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
