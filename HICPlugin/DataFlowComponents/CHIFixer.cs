using System;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Validation.Constraints.Primary;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;

[Description("Attempts to fix the specified CHIColumnName by adding 0 to the front of 9 digit CHIs")]
public partial class CHIFixer : IPluginDataFlowComponent<DataTable>
{
    [DemandsInitialization("The name of the CHI column that is to be adjusted", DemandType = DemandType.Unspecified)]
    public string CHIColumnName { get; set; }

    //must be at least this good (9 digits)
    private static readonly Regex MinimumQualityRegex;

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        foreach (DataRow r in toProcess.Rows)
            if (AdjustChiValue(r[CHIColumnName],out var chi))
                r[CHIColumnName] = chi;

        return toProcess;
    }

    private int _valuesReceived = 0;
    private int _valuesCorrected = 0;
    private int _valuesRejected = 0;

    static CHIFixer()
    {
        MinimumQualityRegex = NineDigits();
    }

    private bool AdjustChiValue(object o,out string chi)
    {
        _valuesReceived++;
        chi = null;

        //if it's null leave it
        if (o == null || o == DBNull.Value)
            return false;

        //if it's not string (e.g. int etc) then toString it
        var valueAsString = o as string ?? o.ToString();

        //if it's blank
        if (string.IsNullOrWhiteSpace(valueAsString))
            return false;


        //it does not match the minimum quality regex reject it
        if (!MinimumQualityRegex.IsMatch(valueAsString))
        {
            _valuesRejected++;
            return false;
        }

        //trim it
        valueAsString = valueAsString.Trim();

            
        //if it is 9 digits make it 10, otherwise give up
        if (valueAsString.Length != 9) return false;

        //try to correct it
        valueAsString = $"0{valueAsString}";

        if (!Chi.IsValidChi(valueAsString, out _)) return false; //could not fix by adding the 0 so just return it as normal

        //correction worked
        _valuesCorrected++;
        chi = valueAsString;
        return true;
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {
            
        listener.OnNotify(this,new NotifyEventArgs(_valuesRejected ==0?ProgressEventType.Information:ProgressEventType.Warning,
            $"Finished adjusting CHIs, we received {_valuesReceived} values for processing, of these {_valuesRejected} were rejected because they did not meet the minimum requirements for processing ({MinimumQualityRegex}).  {_valuesCorrected} values were succesfully fixed by adding a 0 to the front"));
    }

    public void Abort(IDataLoadEventListener listener)
    {
            
    }

    public void Check(ICheckNotifier notifier)
    {
        if (string.IsNullOrWhiteSpace(CHIColumnName))
            notifier.OnCheckPerformed(new CheckEventArgs("CHIColumnName is blank, this component will crash if run", CheckResult.Fail,null));
    }

    [GeneratedRegex("[0-9]{9}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex NineDigits();
}