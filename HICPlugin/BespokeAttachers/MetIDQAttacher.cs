using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataLoadEngine.Attachers;
using DataLoadEngine.Job;
using LoadModules.Generic.DataFlowSources;
using ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using ReusableLibraryCode.Progress;

namespace HICPlugin.BespokeAttachers
{
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
            foreach (var file in job.HICProjectDirectory.ForLoading.GetFiles(FilePattern))
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Started processing file " + file.FullName));

                DelimitedFlatFileDataFlowSource fromCSV = new DelimitedFlatFileDataFlowSource();
                fromCSV.PreInitialize(new FlatFileToLoad(file),job);

                //Read it all in one go
                fromCSV.MaxBatchSize = int.MaxValue;
                fromCSV.GetChunk(job, new GracefulCancellationToken());

                var dt = fromCSV.GetChunk(job, new GracefulCancellationToken());

                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, ""+dt.Rows[0][1]));

                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Found the following headers:" + string.Join(",",dt.Columns.Cast<DataColumn>().Select(c=>c.ColumnName))));
            }

            return ExitCodeType.Error;
        }

        public void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            
        }

        public IHICProjectDirectory HICProjectDirectory { get; set; }
        public bool RequestsExternalDatabaseCreation { get; private set; }
    }
}
