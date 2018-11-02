using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Attachers;
using DataLoadEngine.DataProvider;
using DataLoadEngine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace HICPlugin
{
    /// <summary>
    /// Overrides the default crash behaviour of the RDMP which is to leave remnants (RAW/STAGING) intact for inspection debugging.  This component will predict the staging database and then 
    /// nuke it 
    /// </summary>
    class CrashOverride : IPluginAttacher
    {

        [DemandsInitialization("Attempts to delete all tables relevant to the load in RAW database in the even that the data load crashes",DemandType.Unspecified,true)]
        public bool BurnRAW { get; set; }
        [DemandsInitialization("Attempts to delete all tables relevant to the load in STAGING database in the even that the data load crashes", DemandType.Unspecified, true)]
        public bool BurnSTAGING { get; set; }

        
        private DiscoveredDatabase stagingDatabase;
        List<string> stagingTableNamesToNuke = new List<string>();

        private DiscoveredDatabase rawDatabase;
        List<string> rawTableNamesToNuke = new List<string>();

        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
            if (exitCode == ExitCodeType.Abort || exitCode == ExitCodeType.Error)
            {
                if(BurnSTAGING)
                    DropTables(stagingDatabase, stagingTableNamesToNuke, postLoadEventsListener);

                if(BurnRAW)
                    DropTables(rawDatabase, rawTableNamesToNuke, postLoadEventsListener);
            }
        }

        private void DropTables(DiscoveredDatabase discoveredDatabase, List<string> tables, IDataLoadEventListener postLoadEventsListener)
        {
            if (discoveredDatabase.Exists())
            {
                postLoadEventsListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Found database " + discoveredDatabase));
                foreach (string t in tables)
                {
                    var tbl = discoveredDatabase.ExpectTable(t);
                    if (tbl.Exists())
                    {
                        tbl.Drop();
                        postLoadEventsListener.OnNotify(this,
                            new NotifyEventArgs(ProgressEventType.Information, "Dropped table " + t));
                    }
                    else
                        postLoadEventsListener.OnNotify(this,
                            new NotifyEventArgs(ProgressEventType.Warning, "Did not see table " + t + " in database " + discoveredDatabase.GetRuntimeName()));
                }
            }
            else
                postLoadEventsListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Could not find database "+discoveredDatabase+" for error cleanup"));
        }

        public bool DisposeImmediately { get; set; }
        public void Check(ICheckNotifier notifier)
        {
        }

        public ExitCodeType Attach(IDataLoadJob job,GracefulCancellationToken token)
        {
            foreach (var t in job.LookupTablesToLoad)
                stagingTableNamesToNuke.Add(t.GetRuntimeName(LoadStage.AdjustStaging));

            foreach (var t in job.LookupTablesToLoad)
                rawTableNamesToNuke.Add(t.GetRuntimeName(LoadStage.AdjustRaw));

            stagingDatabase = job.LoadMetadata.GetDistinctLiveDatabaseServer().ExpectDatabase("DLE_STAGING");


            return ExitCodeType.Success;
        }

        public void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            rawDatabase = dbInfo;
        }

        public IHICProjectDirectory HICProjectDirectory { get; set; }
        public bool RequestsExternalDatabaseCreation { get; private set; }
    }
}
