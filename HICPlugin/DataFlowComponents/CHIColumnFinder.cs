using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Annotations;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SixLabors.ImageSharp.Drawing;

namespace HICPluginInteractive.DataFlowComponents;

/// <summary>
/// Pipeline component designed to prevent DataTable columns containing CHIs passing through the pipeline.
/// </summary>
[Description("Crashes the pipeline if any columns are suspected of containing CHIs")]
public sealed partial class CHIColumnFinder : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>, IPipelineRequirement<IBasicActivateItems>
{
    [DemandsInitialization("Component will be shut down until this date and time", DemandType = DemandType.Unspecified)]
    public DateTime? OverrideUntil { get; set; }

    [DemandsInitialization("Will show errors in message boxes for analysis. Leave unticked for unattended execution.", DefaultValue = false, DemandType = DemandType.Unspecified)]
    public bool ShowUIComponents { get; set; }

    [DemandsInitialization("By default all columns from the source will be checked for valid CHIs. Set this to a list of headers (separated with a comma) to ignore the specified columns.", DemandType = DemandType.Unspecified)]
    [NotNull]
    public string IgnoreColumns
    {
        get => string.Join(',', _columnGreenList);
        set => _columnGreenList = (value ?? "").Split(',').Select(static s => s.Trim()).ToList();
    }
    [DemandsInitialization("If populated, RDMP will output and potential CHIs to a file in this directory rather than to the progress logs", DemandType = DemandType.Unspecified)]

    public string OutputFileDirectory { get; set; }

    [DemandsInitialization("If checked, will stop searching for CHIs after it has found 10 of them in the extraction.", DemandType = DemandType.Unspecified)]

    public bool BailOutEarly { get; set; }

    private bool _firstTime = true;

    private List<string> _columnGreenList = new();
    private bool _isTableAlreadyNamed;
    private IBasicActivateItems _activator;

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {

        if (OverrideUntil.HasValue && OverrideUntil.Value > DateTime.Now)
        {
            if (_firstTime)
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"This component is still currently being overridden until the specified date: {OverrideUntil.Value:g}"));
            _firstTime = false;
            return toProcess;
        }
        string fileLocation = null;
        if (!string.IsNullOrWhiteSpace(OutputFileDirectory))
            if (!Directory.Exists(OutputFileDirectory))
            {
                Directory.CreateDirectory(OutputFileDirectory);
            }
        {
            fileLocation = System.IO.Path.Combine(OutputFileDirectory, toProcess.TableName.ToString() + "_Potential_CHI_Locations.csv").ToString();
            if (File.Exists(fileLocation) && BailOutEarly)
            {
                var lineCount = File.ReadLines(fileLocation).Count();
                if (lineCount > 20)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Have skipped this chunk of Catalogue {toProcess.TableName} as there is already a number of CHIs already found"));
                    return toProcess;
                }
            }

        }


        //give the data table the correct name
        if (toProcess.ExtendedProperties.ContainsKey("ProperlyNamed") && toProcess.ExtendedProperties["ProperlyNamed"]?.Equals(true) == true)
            _isTableAlreadyNamed = true;

        if (_columnGreenList.Count != 0)
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"You have chosen the following columns to be ignored: {IgnoreColumns}"));

        var ChiLocations = new List<string>();
        foreach (var col in toProcess.Columns.Cast<DataColumn>().Where(c => !_columnGreenList.Contains(c.ColumnName.Trim())))
        {
            foreach (var val in toProcess.Rows.Cast<DataRow>().Select(DeRef).AsParallel().Where(ContainsValidChi))
            {
                if (!string.IsNullOrWhiteSpace(fileLocation))
                {
                    try
                    {
                        ChiLocations.Add($"{col.ColumnName},{GetPotentialCHI(val)},{val}");
                    }
                    catch (Exception)
                    {
                        ChiLocations.Add($"{col.ColumnName},Unknown,{val}");

                    }
                }
                else if (_activator?.IsInteractive == true && ShowUIComponents)
                {
                    if (DoTheMessageBoxDance(toProcess, listener, col, val))
                        break; // End processing of this whole column
                }
                else
                {
                    var message =
                        $"Column {col.ColumnName} in Dataset {toProcess.TableName} appears to contain a CHI ({val})";
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, message));
                    if (!_isTableAlreadyNamed)
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                            "DataTable has not been named. If you want to know the dataset that the error refers to please add an ExtractCatalogueMetadata to the extraction pipeline."));
                }
            }
            if (ChiLocations.Count != 0)
            {
                ReaderWriterLock locker = new ReaderWriterLock();
                try
                {
                    locker.AcquireWriterLock(int.MaxValue);
                    if (!File.Exists(fileLocation))
                    {
                        using (StreamWriter sw = File.CreateText(fileLocation))
                        {
                            sw.WriteLine("Column,Potential CHI,Value");
                        }
                    }
                    File.AppendAllLines(fileLocation, ChiLocations);
                }
                catch (Exception e)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, e.Message));
                }
                finally
                {
                    locker.ReleaseWriterLock();
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Have Written {ChiLocations.Count} Potential CHIs to {fileLocation}"));

                }
            }

            continue;

            [NotNull]
            string DeRef([NotNull] DataRow row) => row[col].ToString() ?? "";
        }

        return toProcess;
    }


    /// <summary>
    /// Return true if user elected to skip the rest of this column
    /// </summary>
    /// <param name="toProcess"></param>
    /// <param name="listener"></param>
    /// <param name="col"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    private bool DoTheMessageBoxDance(DataTable toProcess, [NotNull] IDataLoadEventListener listener,
        [NotNull] DataColumn col, string val)
    {
        if (_activator.IsInteractive && _activator.YesNo(
                $"Column {col.ColumnName} in Dataset {(_isTableAlreadyNamed ? toProcess.TableName : "UNKNOWN (you need an ExtractCatalogueMetadata in the pipeline to get a proper name)")} appears to contain a CHI ({val})\n\nWould you like to view the current batch of data?",
                "Suspected CHI Column"))
        {

            var txt = UsefulStuff.DataTableToCsv(toProcess);
            _activator.Show("Data", txt);
        }

        if (_activator.YesNo($"Would you like to suppress CHI checking on column {col.ColumnName}?", "Continue extract?"))
        {
            _columnGreenList.Add(col.ColumnName);
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Column {col.ColumnName} will no longer be checked for CHI during the rest of the extract"));
            return true;
        }

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Column {col.ColumnName} will continue to be CHI-checked"));
        return false;
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

    // True if date exists and checksum matches
    private static bool ValidBits(int d, int m, int y, int c)
    {
        c %= 11;
        if (c != 0) return false;

        return m switch
        {
            1 or 3 or 5 or 7 or 8 or 10 or 12 => d is > 0 and < 32,
            4 or 6 or 9 or 11 => d is > 0 and < 31,
            2 => d is > 0 and < 29 || (d == 29 && y % 4 == 0),
            _ => false,
        };
    }

    private enum State
    {
        End,
        Rest1, Rest2, Rest3,
        Year1, Year2,
        Month1, Month2,
        Day1, Day2,
        MaybeSpace,
        Complete
    }

    private static string GetPotentialCHI(string toCheckStr)
    {
        if (string.IsNullOrWhiteSpace(toCheckStr) || toCheckStr.Length < 9) return "";

        var state = State.End; // Start of potential CHI
        int day = 0, month = 0, year = 0, check = 0, indexOfDay = 0;
        for (var i = toCheckStr.Length - 1; i >= 0; i--)
        {
            var c = toCheckStr[i];
            var digit = c - '0';
            if (digit is < 0 or > 9)
            {
                // Non-whitespace: if we're anywhere other than maybe-space, bail.
                switch (state)
                {
                    case State.MaybeSpace:
                        state = char.IsWhiteSpace(c) ? State.Year2 : State.End;
                        break;

                    case State.Day1: // Might be a 9 digit "CHI" with leading zero removed
                    case State.Complete:
                        state = State.End;
                        if (ValidBits(day, month, year, check))
                            return toCheckStr.Substring(i + 1, 9);

                        break;

                    default:
                        state = State.End;
                        break;
                }
                continue;
            }

            // OK, we got a digit. What does it mean in the current state?
            switch (state)
            {
                case State.End:
                    check = digit;
                    state = State.Rest3;
                    break;

                case State.Rest3:
                    check += digit * 2;
                    state = State.Rest2;
                    break;

                case State.Rest2:
                    check += digit * 3;
                    state = State.Rest1;
                    break;

                case State.Rest1:
                    check += digit * 4;
                    state = State.MaybeSpace;
                    break;

                case State.MaybeSpace:
                case State.Year2:
                    check += digit * 5;
                    year = digit;
                    state = State.Year1;
                    break;

                case State.Year1:
                    check += digit * 6;
                    year += digit * 10;
                    state = State.Month2;
                    break;

                case State.Month2:
                    check += digit * 7;
                    month = digit;
                    state = State.Month1;
                    break;

                case State.Month1:
                    check += digit * 8;
                    month += digit * 10;
                    state = State.Day2;
                    break;

                case State.Day2:
                    check += digit * 9;
                    day = digit;
                    indexOfDay = i;
                    state = State.Day1;
                    break;

                case State.Day1:
                    check += digit * 10;
                    day += 10 * digit;
                    indexOfDay = i;
                    state = State.Complete;
                    break;

                case State.Complete:
                    // More than 10 digits - just keep consuming, cannot possibly be valid now.
                    day = 32;
                    break;
            }
        }
        return ValidBits(day, month, year, check) ? toCheckStr.Substring(indexOfDay, 10) : "";
    }

    private static bool ContainsValidChi([CanBeNull] object toCheck)
    {
        if (toCheck == null || toCheck == DBNull.Value)
            return false;

        var toCheckStr = toCheck.ToString();
        if (toCheckStr is null || toCheckStr.Length < 9) return false;

        var state = State.End; // Start of potential CHI
        int day = 0, month = 0, year = 0, check = 0;
        for (var i = toCheckStr.Length - 1; i >= 0; i--)
        {
            var c = toCheckStr[i];
            var digit = c - '0';
            if (digit is < 0 or > 9)
            {
                // Non-whitespace: if we're anywhere other than maybe-space, bail.
                switch (state)
                {
                    case State.MaybeSpace:
                        state = char.IsWhiteSpace(c) ? State.Year2 : State.End;
                        break;

                    case State.Day1: // Might be a 9 digit "CHI" with leading zero removed
                    case State.Complete:
                        state = State.End;
                        if (ValidBits(day, month, year, check))
                            return true;

                        break;

                    default:
                        state = State.End;
                        break;
                }
                continue;
            }

            // OK, we got a digit. What does it mean in the current state?
            switch (state)
            {
                case State.End:
                    check = digit;
                    state = State.Rest3;
                    break;

                case State.Rest3:
                    check += digit * 2;
                    state = State.Rest2;
                    break;

                case State.Rest2:
                    check += digit * 3;
                    state = State.Rest1;
                    break;

                case State.Rest1:
                    check += digit * 4;
                    state = State.MaybeSpace;
                    break;

                case State.MaybeSpace:
                case State.Year2:
                    check += digit * 5;
                    year = digit;
                    state = State.Year1;
                    break;

                case State.Year1:
                    check += digit * 6;
                    year += digit * 10;
                    state = State.Month2;
                    break;

                case State.Month2:
                    check += digit * 7;
                    month = digit;
                    state = State.Month1;
                    break;

                case State.Month1:
                    check += digit * 8;
                    month += digit * 10;
                    state = State.Day2;
                    break;

                case State.Day2:
                    check += digit * 9;
                    day = digit;
                    state = State.Day1;
                    break;

                case State.Day1:
                    check += digit * 10;
                    day += 10 * digit;
                    state = State.Complete;
                    break;

                case State.Complete:
                    // More than 10 digits - just keep consuming, cannot possibly be valid now.
                    day = 32;
                    break;
            }
        }
        return ValidBits(day, month, year, check);
    }

    public void PreInitialize(IExtractCommand value, IDataLoadEventListener listener)
    {
        if (value is not ExtractDatasetCommand edcs) return;

        try
        {
            var hashOnReleaseColumns = edcs.Catalogue.CatalogueItems.Select(static ci => ci.ExtractionInformation)
                .Where(static ei => ei?.HashOnDataRelease == true).Select(static ei => ei.GetRuntimeName()).ToArray();

            if (!hashOnReleaseColumns.Any()) return;

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Ignoring the following columns as they have been hashed on release: {string.Join(", ", hashOnReleaseColumns)}"));
            _columnGreenList.AddRange(hashOnReleaseColumns);
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