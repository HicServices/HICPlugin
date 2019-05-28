using System;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    [Description("Forces tables being loaded to match the hic regex ")]
    public class ForceHICTableNamingConventionForProjects : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<TableInfo>
    {
        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener job, GracefulCancellationToken cancellationToken)
        {
            return toProcess;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {

        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public void PreInitialize(TableInfo target,IDataLoadEventListener listener)
        {
            Regex namingConvention = new Regex("tt_\\d*");

            if (!namingConvention.IsMatch(target.GetRuntimeName()))
                listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error, "TableInfo " + target + " does not match hic regex for naming conventions of project/group data (" + namingConvention + ")"));
        }

        
        public void Check(ICheckNotifier notifier)
        {
            
        }
    }
}
