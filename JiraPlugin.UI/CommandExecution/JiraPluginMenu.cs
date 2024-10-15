using JiraPlugin.UI.CommandExecution.AtomicCommands;
using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.DataExport.Data;

namespace JiraPlugin.UI.CommandExecution;

public class JiraPluginMenu : PluginUserInterface
{
    readonly IBasicActivateItems _activator;

    public JiraPluginMenu(IBasicActivateItems itemActivator) : base(itemActivator)
    {
        _activator = itemActivator;
    }

    public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
    {
        if (_activator != null && o is Project)
        {
            return [
                new ExecuteCommandLinkProjectToJiraAsset(_activator,(Project)o),
                new ExecuteCommandViewLinkedExternalAssetsForProject(_activator,(Project)o)
            ];
        }

        return base.GetAdditionalRightClickMenuItems(o);
    }
}
