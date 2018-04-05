using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatalogueLibrary.DataFlowPipeline;
using HIC.Common.Validation.Constraints.Primary;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    /// <summary>
    /// Pipeline component designed to prevent DataTable columns containing CHIs passing through the pipeline. The component will crash the entire pipeline 
    /// if it finds columns which are valid CHIs.
    /// This will only detect values which are CHI in a whole i.e. it won't match a CHI in a list/paragraph/free text etc.
    /// </summary>
    [Description("Crashes the pipeline if any columns are suspected of containing CHIs")]
    public class CHIColumnFinder : IPluginDataFlowComponent<DataTable>
    {
        private int _rowCount;

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,
            GracefulCancellationToken cancellationToken)
        {
            foreach (var row in toProcess.Rows.Cast<DataRow>())
            {
                _rowCount++;
                foreach (var col in toProcess.Columns.Cast<DataColumn>())
                {
                    if (IsValidChi(row[col]))
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

        private bool IsValidChi(object toCheck)
        {
            if (toCheck == null || toCheck == DBNull.Value)
                return false;

            var toCheckStr = toCheck.ToString().Trim();
            if (String.IsNullOrWhiteSpace(toCheckStr))
                return false;

            string outString;
            return Chi.IsValidChi(toCheckStr, out outString);
        }
    }
}
