using GoDartsPluginUI.CommandExecution.AtomicCommands;

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