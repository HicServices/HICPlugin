using GoDartsPluginUI.CommandExecution.AtomicCommands;
using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Providers.Nodes;
using Rdmp.UI.ItemActivation;
using System.Collections.Generic;

namespace GoDartsPluginUI.CommandExecution
{
    public class GoDartsPluginMenu : PluginUserInterface
    {
        IActivateItems activator;

        public GoDartsPluginMenu(IBasicActivateItems itemActivator) : base(itemActivator)
        {
            activator = itemActivator as IActivateItems;
        }

        public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
        {
            if(activator != null && o is AllServersNode)
            {
                return new[] { new ExecuteCommandSetupGoFusionFromDatabase(activator) };
            }

            return base.GetAdditionalRightClickMenuItems(o);
        }
    }
}