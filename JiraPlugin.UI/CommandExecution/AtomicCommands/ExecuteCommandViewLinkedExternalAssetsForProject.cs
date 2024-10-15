using JiraPlugin.UI.JiraUI;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.ItemActivation;

namespace JiraPlugin.UI.CommandExecution.AtomicCommands;

public class ExecuteCommandViewLinkedExternalAssetsForProject: BasicCommandExecution, IAtomicCommand
{

    private readonly IActivateItems _activator;
    private readonly Project _project;

    public ExecuteCommandViewLinkedExternalAssetsForProject(IBasicActivateItems activator, Project project) {
        _activator = activator as IActivateItems;
        _project = project;
    }

    public override void Execute()
    {
        base.Execute();
        _activator.Activate<ViewProjectJiraAssets, Project>(_project);
    }
}
