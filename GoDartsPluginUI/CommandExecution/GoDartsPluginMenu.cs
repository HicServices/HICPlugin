using GoDartsPluginUI.CommandExecution.AtomicCommands;
using Rdmp.Core;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Providers.Nodes;
using Rdmp.UI.ItemActivation;
using System.Collections.Generic;

namespace GoDartsPluginUI.CommandExecution
{
    public class GoDartsPluginMenu : PluginUserInterface
    {
        public GoDartsPluginMenu(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
        {
            var serverNode = o as AllServersNode;
            if (serverNode == null)
                return null;

            if(BasicActivator is IActivateItems a)
            {
                return new[] { new ExecuteCommandSetupGoFusionFromDatabase(a) };
            }
            return base.GetAdditionalRightClickMenuItems(o);
        }
    }
}