using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using HIC.Common.Validation.Constraints.Primary;
using HICPluginInteractive.UIComponents;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using ReusableUIComponents.SingleControlForms;

namespace HICPluginInteractive.DataFlowComponents
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
        
        private List<string> _columnWhitelist = new List<string>();

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if (OverrideUntil.HasValue && OverrideUntil.Value > DateTime.Now)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "This component is still currently being overridden until the specified date: " + OverrideUntil.Value.ToString("g")));
                return toProcess;
            }

            var batchRowCount = 0;
            var dtRows = toProcess.Rows.Cast<DataRow>().ToArray();
            foreach (var row in dtRows)
            {
                foreach (var col in toProcess.Columns.Cast<DataColumn>())
                {
                    if (!_columnWhitelist.Contains(col.ColumnName) && ContainsValidChi(row[col]))
                    {
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Column " + col.ColumnName + " appears to contain a CHI (" + row[col] + ")"));

                        if (MessageBox.Show("Column " + col.ColumnName + " appears to contain a CHI (" + row[col] + ")\n\nWould you like to view the current batch of data?", "Suspected CHI Column", MessageBoxButtons.YesNo) == DialogResult.Yes) 
                        {
                            var dtv = new ExtractDataTableViewer(toProcess, "View data", col.ColumnName, batchRowCount);
                            SingleControlForm.ShowDialog(dtv);
                        }

                        if (MessageBox.Show("Would you like to suppress CHI checking on column " + col.ColumnName + " and continue extract?", "Continue extract?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            _columnWhitelist.Add(col.ColumnName);
                            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, col.ColumnName + " will no longer be checked for CHI during the rest of the extract"));
                        }
                        else
                        {
                            throw new Exception("Extract abandoned by user");
                        }
                    }
                }

                batchRowCount++;
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
