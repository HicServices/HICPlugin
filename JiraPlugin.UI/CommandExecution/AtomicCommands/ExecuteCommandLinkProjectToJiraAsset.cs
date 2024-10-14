using InterfaceToJira.RestApiClient2.JiraModel;
using JiraPlugin.UI.JiraUI;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.ExtractionUIs.JoinsAndLookups;
using Rdmp.UI.ItemActivation;
namespace JiraPlugin.UI.CommandExecution.AtomicCommands;

public class ExecuteCommandLinkProjectToJiraAsset: BasicCommandExecution, IAtomicCommand
{
    private IActivateItems _activator;
    private Project _project;
    public ExecuteCommandLinkProjectToJiraAsset(IBasicActivateItems activator, Project project) {
        _activator = activator as IActivateItems;
        _project = project;
    }

    public override void Execute()
    {
        base.Execute();
        _activator.Activate<LinkProjectToJiraAssetUI, Project>(_project);
    }
}
