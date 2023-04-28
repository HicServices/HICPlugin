using FAnsi.Discovery;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DataProvider.FromCache;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.Job.Scheduling;

namespace SCIStorePlugin.DataProvider;

public class SCIStoreCachedFileRetriever : CachedFileRetriever
{
    private ScheduledDataLoadJob _scheduledJob;

    public override void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
            
    }

    public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
    {
        _scheduledJob = ConvertToScheduledJob(dataLoadJob);

        var jobs = GetDataLoadWorkload(_scheduledJob);

        ExtractJobs(_scheduledJob);

        _scheduledJob.PushForDisposal(new DeleteCachedFilesOperation(_scheduledJob, jobs));
        _scheduledJob.PushForDisposal(new UpdateProgressIfLoadsuccessful(_scheduledJob));

        return ExitCodeType.Success;
    }
}