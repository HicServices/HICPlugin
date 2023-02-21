using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.Validation.Constraints.Primary;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace HICPluginInteractive.DataFlowComponents;

/// <summary>
/// Pipeline component designed to prevent DataTable columns containing CHIs passing through the pipeline. The component will crash the entire pipeline 
/// if it finds columns which contain valid CHIs.
/// </summary>
[Description("Crashes the pipeline if any columns are suspected of containing CHIs")]
public class CHIColumnFinder : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>, IPipelineRequirement<IBasicActivateItems>
{
    [DemandsInitialization("Component will be shut down until this date and time", DemandType = DemandType.Unspecified)]
    public DateTime? OverrideUntil { get; set; }

    [DemandsInitialization("Will show errors in messageboxes for analysis. Leave unticked for unattended execution.", DefaultValue = false, DemandType = DemandType.Unspecified)]
    public bool ShowUIComponents { get; set; }

    [DemandsInitialization("By default all columns from the source will be checked for valid CHIs. Set this to a list of headers (separated with a comma) to ignore the specified columns.", DemandType = DemandType.Unspecified)]
    public string IgnoreColumns { get; set; }

    private bool _firstTime = true;

    private List<string> _columnWhitelist = new List<string>();
    private List<string> _foundChiList = new List<string>();
    private bool _isTableAlreadyNamed;
    private IBasicActivateItems _activator;

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (OverrideUntil.HasValue && OverrideUntil.Value > DateTime.Now)
        {
            if (_firstTime)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                    $"This component is still currently being overridden until the specified date: {OverrideUntil.Value:g}"));
                _firstTime = false;
            }
            return toProcess;
        }

        //give the data table the correct name
        if (toProcess.ExtendedProperties.ContainsKey("ProperlyNamed") && toProcess.ExtendedProperties["ProperlyNamed"].Equals(true))
            _isTableAlreadyNamed = true;

        if (!string.IsNullOrEmpty(IgnoreColumns))
        {
            var ignoreColumnsArray = IgnoreColumns.Split(new[] { ',' }).Select(s => s.Trim()).ToArray();

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"You have chosen the following columns to be ignored: {string.Join(", ", ignoreColumnsArray)}"));
            _columnWhitelist.AddRange(ignoreColumnsArray);
        }

        var batchRowCount = 0;
        var dtRows = toProcess.Rows.Cast<DataRow>().ToArray();
        foreach (var row in dtRows)
        {
            foreach (var col in toProcess.Columns.Cast<DataColumn>())
            {
                if (_columnWhitelist.Contains(col.ColumnName.Trim()) || !ContainsValidChi(row[col])) continue;
                if (_activator.IsInteractive && ShowUIComponents)
                    DoTheMessageBoxDance(toProcess, listener, col, row, batchRowCount);
                else
                {
                    var message =
                        $"Column {col.ColumnName} in Dataset {toProcess.TableName} appears to contain a CHI ({row[col]})";
                    _foundChiList.Add(message);
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, message));
                    if (!_isTableAlreadyNamed)
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                            "DataTable has not been named. If you want to know the dataset that the error refers to please add an ExtractCatalogueMetadata to the extraction pipeline."));
                }
            }

            batchRowCount++;
        }

        return toProcess;
    }
        

    private void DoTheMessageBoxDance(DataTable toProcess, IDataLoadEventListener listener, DataColumn col, DataRow row, int batchRowCount) 
    {
        if (_activator.IsInteractive && _activator.YesNo(
                $"Column {col.ColumnName} in Dataset {(_isTableAlreadyNamed ? toProcess.TableName : "UNKNOWN (you need an ExtractCatalogueMetadata in the pipeline to get a proper name)")} appears to contain a CHI ({row[col]})\n\nWould you like to view the current batch of data?", "Suspected CHI Column"))
        {

            var txt = UsefulStuff.GetInstance().DataTableToCsv(toProcess);
            _activator.Show("Data", txt);
        }

        if (_activator.YesNo($"Would you like to suppress CHI checking on column {col.ColumnName}?", "Continue extract?"))
        {
            _columnWhitelist.Add(col.ColumnName);
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Column {col.ColumnName} will no longer be checked for CHI during the rest of the extract"));
        }
        else
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Column {col.ColumnName} will continue to be CHI-checked"));
        }
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

    private bool ContainsValidChi(object toCheck)
    {
        if (toCheck == null || toCheck == DBNull.Value)
            return false;

        var toCheckStr = toCheck.ToString();
        if (string.IsNullOrWhiteSpace(toCheckStr))
            return false;

        var candidates = Regex.Matches(toCheckStr, @"(?<!\d)\d{10}(?!\d)|(?<!\d)\d{9}(?!\d)").Cast<Match>().Select(m => m.Value).ToList();

        var regexSplitCandidates = Regex.Matches(toCheckStr, @"(?<!\d)(\d{6})(?!\d)\s(?<!\d)(\d{4})(?!\d)|(?<!\d)(\d{5})(?!\d)\s(?<!\d)(\d{4})(?!\d)").Cast<Match>().ToList();
        var tenDigitSplit = regexSplitCandidates.Select(m => m.Groups[1].Value + m.Groups[2].Value);
        candidates.AddRange(tenDigitSplit);
        var nineDigitSplit = regexSplitCandidates.Select(m => m.Groups[3].Value + m.Groups[4].Value);
        candidates.AddRange(nineDigitSplit);

        foreach (var candidate in candidates.ToArray())
        {
            var prefix = "";
            if (candidate.Length == 9)
                prefix = "0";

            if (Chi.IsValidChi(prefix + candidate, out var outString))
                return true;
        }

        return false;
    }

    public void PreInitialize(IExtractCommand value, IDataLoadEventListener listener)
    {
        if (value is not ExtractDatasetCommand edcs) return;
        try
        {
            var hashOnReleaseColumns = edcs.Catalogue.CatalogueItems.Select(ci => ci.ExtractionInformation).Where(ei => ei != null && ei.HashOnDataRelease).Select(ei => ei.GetRuntimeName()).ToArray();

            if (!hashOnReleaseColumns.Any()) return;
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Ignoring the following columns as they have been hashed on release: {string.Join(", ", hashOnReleaseColumns)}"));
            _columnWhitelist.AddRange(hashOnReleaseColumns);
        }
        catch (Exception e)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Failed to get HashOnDataRelease columns for catalogue {edcs.Catalogue.Name} with the exception: {e.Message} Columns can be ignored manually using the Ignore Columns option", e));
        }
    }

    public void PreInitialize(IBasicActivateItems value, IDataLoadEventListener listener)
    {
        _activator = value;
    }
}