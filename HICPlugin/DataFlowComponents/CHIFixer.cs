using System;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using HIC.Common.Validation.Constraints.Primary;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    [Description("Attempts to fix the specified CHIColumnName by adding 0 to the front of 9 digit CHIs")]
    public class CHIFixer : IPluginDataFlowComponent<DataTable>
    {
        [DemandsInitialization("The name of the CHI column that is to be adjusted", DemandType = DemandType.Unspecified)]
        public string CHIColumnName { get; set; }

        //must be at least this good (9 digits)
        Regex minimumQualityRegex = new Regex("[0-9]{9}");

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            foreach (DataRow r in toProcess.Rows)
                r[CHIColumnName] = AdjustCHIValue(r[CHIColumnName]);

            return toProcess;
        }

        private int valuesReceived = 0;
        private int valuesCorrected = 0;
        private int valuesRejected = 0;

        private object AdjustCHIValue(object o)
        {
            valuesReceived++;

            //if it's null leave it
            if (o == null || o == DBNull.Value)
                return o;

            //if its not string (e.g. int etc) then tostring it
            string valueAsString = o as string ?? o.ToString();

            //if it's blank
            if (string.IsNullOrWhiteSpace(valueAsString))
                return valueAsString;


            //it does not match the minimum quality regex reject it
            if (!minimumQualityRegex.IsMatch(valueAsString))
            {
                valuesRejected++;
                return o;
            }

            //trim it
            valueAsString = valueAsString.Trim();

            
            //if it is 9 digits make it 10
            if (valueAsString.Length == 9)
            {
                //try to correct it
                valueAsString = "0" + valueAsString;

                string whoCares;
                if(Chi.IsValidChi(valueAsString,out whoCares))
                {
                    //correction worked
                    valuesCorrected++;
                    return valueAsString;
                }
                return o;//could not fix by adding the 0 so just return it as normal
            }
            
            return valueAsString;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            
            listener.OnNotify(this,new NotifyEventArgs(valuesRejected ==0?ProgressEventType.Information:ProgressEventType.Warning,
                    "Finished asjusting CHIs, we received " + valuesReceived + " values for processing, of these " + valuesRejected + " were rejected because they did not meet the minimum requirements for processing ("+minimumQualityRegex+").  " + valuesCorrected + " values were succesfully fixed by adding a 0 to the front" ));
        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public bool SilentRunning { get; set; }
        public void Check(ICheckNotifier notifier)
        {
            if (string.IsNullOrWhiteSpace(CHIColumnName))
                notifier.OnCheckPerformed(new CheckEventArgs("CHIColumnName is blank, this component will crash if run", CheckResult.Fail,null));
        }
    }
}
