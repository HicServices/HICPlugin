using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;
using Rdmp.Core.Validation.Constraints.Secondary;

namespace SCIStorePlugin.Attachers;

[Description(@"Populates the RAW database from XML files retrieved from the SCI Store web service in the ForLoading directory")]
public class SciStoreAttacher : Attacher
{
    public string HeaderTable = "Header";
    public string SamplesTable = "SampleDetails";
    public string ResultsTable = "Results";
    private Stopwatch _timer;
    private ReferentialIntegrityConstraint _readCodeConstraint;

    [DemandsInitialization("Column that contains Read codes for validation checking when creating lab Results objects.")]
    public ColumnInfo ReadCodeLookupColumn { get; set; }

    [DemandsInitialization(@"Determines behaviour when bad reports are encountered e.g. LabNumbers like 000000000.  
True - Report warning and continue (not loading file)
False - Stop the data load with an error", DefaultValue = true)]
    public bool IgnoreBadData { get; set; }

    public SciStoreAttacher()
        : base(true)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    /// <exception cref="BadCombinedReportDataException"></exception>
    public override ExitCodeType Attach(IDataLoadJob job, GracefulCancellationToken token)
    {
        // Create the DataTable repository into which the XML files will be loaded into prior to their bulk insert into RAW
        var dataTableSchemaSource = new DataTableSchemaFromDatabase(_dbInfo);
        var destRepo = new SciStoreDataTableRepository(dataTableSchemaSource);
        destRepo.InsertionError += (sender, e, query) => job.OnNotify(sender,
            new NotifyEventArgs(ProgressEventType.Warning,
                $"Received InsertionError from {nameof(SciStoreDbRepository)} {e}{Environment.NewLine}Query: {query}", e));

        try
        {
            // Attempt to read directly from the archives without uncompressing the files first
            var srcRepo = new CombinedReportArchivedXmlRepository(LoadDirectory.ForLoading);

            job.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Information,
                    $"Reading the CombinedReport XML files from {LoadDirectory.ForLoading}"));
            var reports = srcRepo.ReadAll().ToList();

            // Don't bother going any further if there are no reports!
            if (reports.Count == 0)
                return ExitCodeType.Success;

            // Create the list of SciStoreReports
            CreateReadCodeConstraint();
            var reportFactory = new SciStoreReportFactory(_readCodeConstraint) { IgnoreBadData = IgnoreBadData };

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Reading the lab files as a list of SciStoreReport objects."));
            List<SciStoreReport> labs;
            try
            {
                labs = reports
                    .Select(report => reportFactory.Create(report, job))
                    .Where(r => r != null) //bad reports are null
                    .ToList();
            }
            catch (BadCombinedReportDataException e)
            {
                // this used to log errors and continue but we really just want to fail fast and fix any errors in the parsing/understanding of the XML files
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                    $"Bad report detected when running attacher: {e}", e));
                var archiveFilepath = srcRepo.FindArchiveContainingReport(e.BadReport);
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                    $"Bad report lives in archive at: {archiveFilepath}"));
                throw;
            }

            _timer = new Stopwatch();
            _timer.Start();

            ResolveTestResultOrderDuplication(labs, job);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Writing the in-memory SciStoreReport objects into Data Tables."));
            destRepo.Create(labs, job);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"{destRepo.HeadersTable.Rows.Count} rows added to Header Data Table"));
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"{destRepo.SampleDetailsTable.Rows.Count} rows added to Sample Details Data Table"));
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"{destRepo.ResultsTable.Rows.Count} rows added to Results Data Table"));

            // this repo uses DataTables, so next need to batch insert the table data into the DB
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Starting to insert headers"));

            BulkInsertDataTable(destRepo.HeadersTable, job);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Completed inserting headers"));
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Starting to insert sample details"));

            BulkInsertDataTable(destRepo.SampleDetailsTable, job);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Completed inserting sample details"));
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Starting to insert results"));

            BulkInsertDataTable(destRepo.ResultsTable, job);

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Completed inserting results"));


            _timer.Stop();

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SciStoreAttacher::Attach complete"));
        }
        catch (Exception e)
        {
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error,
                $"{nameof(SciStoreAttacher)}.Attach() failed ", e));
            return ExitCodeType.Error;
        }

        return ExitCodeType.Success;
    }

    private void BulkInsertDataTable(DataTable dataTable, IDataLoadJob job)
    {
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Bulk inserting data in DataTable ({dataTable.TableName}) to {_dbInfo.Server.Name}, {_dbInfo.GetRuntimeName()}..{dataTable.TableName}"));

        var tbl = _dbInfo.ExpectTable(dataTable.TableName);
        using var blk = tbl.BeginBulkInsert();
        DataTable ndt = dataTable.Clone();
        foreach (var row in dataTable.Rows)
        {
            try
            {
                ndt = dataTable.Clone();
                ndt.Rows.Add(row);
                blk.Upload(ndt);
            }
            catch (Exception e)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
               $"Skipped a row due to error: {e.Message}.", e));
            }
        }
        //blk.Upload(dataTable);
    }

    public bool SilentRunning { get; set; }

    public override void Check(ICheckNotifier checker)
    {
        // Has ReadCodeLookupColumn been set?
        if (ReadCodeLookupColumn == null)
            checker.OnCheckPerformed(new CheckEventArgs("ReadCodeLookupColumn cannot be null, make sure it is set to a column containing Read Codes for validation", CheckResult.Fail));

        // Ask the constraint to check itself using our ReadCodeLookupColumn
        CreateReadCodeConstraint();
        _readCodeConstraint.Check(checker);
    }

    /// <summary>
    /// Ensure we only create the read code constraint once for checking/attaching. It contains a potentially expensive caching operation that we only want to execute once during the checking and attaching process.
    /// </summary>
    private void CreateReadCodeConstraint()
    {
        if (_readCodeConstraint != null) return;

        _readCodeConstraint = new ReferentialIntegrityConstraint
        {
            OtherColumnInfo = ReadCodeLookupColumn
        };
    }

    public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventListener)
    {
    }

    private void ResolveTestResultOrderDuplication(List<SciStoreReport> labs, IDataLoadEventListener job)
    {
        foreach (var lab in labs)
        {
            foreach (var sample in lab.Samples)
            {
                var recordsRemoved = sample.ResolveTestResultOrderDuplication();
                if (recordsRemoved > 0)
                    job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                        $"Resolved duplicate TestResultOrder using method 'ResolveTestResultOrderDuplication' in lab number:{lab.Header.LabNumber}"));
            }
        }
    }
}

class PreLoadConfiguration : SciStoreTableRecord
{
    public int CatalogId { get; set; }
}