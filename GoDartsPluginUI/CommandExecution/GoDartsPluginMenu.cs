using GoDartsPluginUI.CommandExecution.AtomicCommands;
using Rdmp.Core.Providers.Nodes;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.PluginChildProvision;
using System.Windows.Forms;

namespace GoDartsPluginUI.CommandExecution
{
    public class GoDartsPluginMenu : PluginUserInterface
    {
        public GoDartsPluginMenu(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override ToolStripMenuItem[] GetAdditionalRightClickMenuItems(object o)
        {
            var serverNode = o as AllServersNode;
            if (serverNode == null)
                return null;

            return GetMenuArray(new ExecuteCommandSetupGoFusionFromDatabase(ItemActivator));
        }
    }
}