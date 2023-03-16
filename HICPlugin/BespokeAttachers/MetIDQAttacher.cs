using System.Data;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.DataLoad;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataFlowPipeline.Requirements;

namespace HICPlugin.BespokeAttachers;

public class MetIDQAttacher : IPluginAttacher
{
    [DemandsInitialization("File pattern to load")]
    public string FilePattern { get; set; }



    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
            
    }

    public bool DisposeImmediately { get; set; }
    public void Check(ICheckNotifier notifier)
    {
            
    }

    public ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
    {
        foreach (var file in job.LoadDirectory.ForLoading.GetFiles(FilePattern))
        {
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Started processing file {file.FullName}"));

            DelimitedFlatFileDataFlowSource fromCSV = new DelimitedFlatFileDataFlowSource();
            fromCSV.PreInitialize(new FlatFileToLoad(file),job);

            //Read it all in one go
            fromCSV.MaxBatchSize = int.MaxValue;
            fromCSV.GetChunk(job, new GracefulCancellationToken());

            var dt = fromCSV.GetChunk(job, new GracefulCancellationToken());

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"{dt.Rows[0][1]}"));

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Found the following headers:{string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}"));
        }

        return ExitCodeType.Error;
    }

    public void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
            
    }

    public ILoadDirectory LoadDirectory { get; set; }
    public bool RequestsExternalDatabaseCreation { get; private set; }
}