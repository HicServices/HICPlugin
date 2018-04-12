using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using HIC.Common.Validation.Constraints.Primary;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    /// <summary>
    /// Pipeline component designed to prevent DataTable columns containing CHIs passing through the pipeline. The component will crash the entire pipeline 
    /// if it finds columns which contain valid CHIs.
    /// </summary>
    [Description("Crashes the pipeline if any columns are suspected of containing CHIs")]
    public class CHIColumnFinder : IPluginDataFlowComponent<DataTable>
    {
        [DemandsInitialization("Component will be shut down until this date and time", DemandType = DemandType.Unspecified)]
        public DateTime? OverrideUntil { get; set; }

        private int _rowCount = 1; //start at 1 as the first row contains the headers

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,
            GracefulCancellationToken cancellationToken)
        {
            if (OverrideUntil.HasValue && OverrideUntil.Value > DateTime.Now)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "This component is still currently being overridden until the specified date: " + OverrideUntil.Value.ToString("g")));
                return toProcess;
            }

            foreach (var row in toProcess.Rows.Cast<DataRow>())
            {
                _rowCount++;
                foreach (var col in toProcess.Columns.Cast<DataColumn>())
                {
                    if (ContainsValidChi(row[col]))
                        throw new Exception("Found CHI  in column " + col.ColumnName + " on row " + _rowCount + "(" + row[col] + ")");
                }
            }
            
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

        private bool ContainsValidChi(object toCheck)
        {
            if (toCheck == null || toCheck == DBNull.Value)
                return false;

            var toCheckStr = toCheck.ToString();
            if (String.IsNullOrWhiteSpace(toCheckStr))
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

                string outString;
                if (Chi.IsValidChi(prefix + candidate, out outString))
                    return true;
            }

            return false;
        }
    }
}
