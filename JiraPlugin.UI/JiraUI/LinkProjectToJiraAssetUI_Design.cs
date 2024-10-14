using Rdmp.UI.TestsAndSetup.ServicePropogation;
using Rdmp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rdmp.Core.DataExport.Data;

namespace JiraPlugin.UI.JiraUI;


[TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<LinkProjectToJiraAssetUI_Design, UserControl>))]
public abstract class LinkProjectToJiraAssetUI_Design : RDMPSingleDatabaseObjectControl<Project>
{
}