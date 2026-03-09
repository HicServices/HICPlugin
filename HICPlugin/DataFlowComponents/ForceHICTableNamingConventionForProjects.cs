using System;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;

[Description("Forces tables being loaded to match the hic regex ")]
public partial class ForceHICTableNamingConventionForProjects : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<TableInfo>
{
    private static readonly Regex NamingConvention = TtPrefix();

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

    public void Check(ICheckNotifier notifier)
    {
            
    }

    [GeneratedRegex("tt_\\d*", RegexOptions.Compiled)]
    private static partial Regex TtPrefix();

    public void PreInitialize(IBasicActivateItems activator, TableInfo value, IDataLoadEventListener listener)
    {
        if (!NamingConvention.IsMatch(value.GetRuntimeName()))
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                $"TableInfo {value} does not match hic regex for naming conventions of project/group data ({NamingConvention})"));
    }
}