using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Rdmp.Core.ReusableLibraryCode;
using SCIStore.SciStoreServices81;
using SCIStorePlugin.Data;

namespace SCIStorePlugin;

/// <summary>
/// This class holds helper methods for cleaning/transforming general to any SciStore dataset
/// </summary>
public class SciStoreHelper
{
    public static string CreateClinicalDetailsField(string[] clinicalDataRequired)
    {
        if (clinicalDataRequired == null) return null;
        var sb = new StringBuilder();
        foreach (var st in clinicalDataRequired)
            sb.Append($"{st.Replace("REQUESTOR", "").Replace("*", "")} ");
        return Clean(sb.ToString());
    }

    public static string CreateLabCommentField(string[] comment)
    {
        return comment == null ? null : Clean(string.Join(" ", comment));
    }

    private static string Clean(string toClean)
    {
        // TODO: Why is this cleaning logic different from that in SciStoreResult?
        return toClean.Trim()
            .Replace("'", " ");

    }

    /// <summary>
    /// Gets Read code and description from the TestName field
    /// </summary>
    /// <param name="test">TEST_TYPE record</param>
    /// <returns>Code, Testn name and Read code</returns>
    public static TestResultNames ParseTestCode(TEST_TYPE test)
    {
        if (test.TestName[0].Item is not CLINICAL_INFORMATION_TYPE clinicalInformationType)
            throw new Exception("Could not cast test.TestName[0].Item as CLINICAL_INFORMATION_TYPE");

        var tr = new TestResultNames
        {
            ReadCode = clinicalInformationType.ClinicalCode.ClinicalCodeValue[0],
            TestName = clinicalInformationType.ClinicalCodeDescription
        };

        if (test.TestName.Length == 1)
            tr.Code = tr.ReadCode;
        else
        {
            tr.Code = test.TestName[1].Item is CLINICAL_INFORMATION_TYPE type ? type.ClinicalCode.ClinicalCodeValue[0] : test.TestName[1].Item.ToString();
        }

        return tr;
    }

    public static string FaultToVerboseData(FaultException faultException)
    {
        var toReturn = "";

        toReturn += $"Action:{faultException.Action}{Environment.NewLine}";
            
        toReturn += $"\tCode.IsPredefinedFault:{faultException.Code.IsPredefinedFault}{Environment.NewLine}";
        toReturn += $"\tCode.IsReceiverFault:{faultException.Code.IsReceiverFault}{Environment.NewLine}";
        toReturn += $"\tCode.IsSenderFault:{faultException.Code.IsSenderFault}{Environment.NewLine}";
        toReturn += $"\tCode.Name:{faultException.Code.Name}{Environment.NewLine}";
        toReturn += $"\tCode.Namespace:{faultException.Code.Namespace}{Environment.NewLine}";
        toReturn += $"\tCode.SubCode:{faultException.Code.SubCode}{Environment.NewLine}";

        toReturn += $"Message:{faultException.Message}{Environment.NewLine}";
        toReturn += $"Reason:{faultException.Reason}{Environment.NewLine}";

        toReturn += Environment.NewLine;

        try
        {
            var messageFault = faultException.CreateMessageFault();

            toReturn += $"\tMessageFault.Actor:{messageFault.Actor}{Environment.NewLine}";


            toReturn +=
                $"\t\tCreateMessageFault().Code.IsPredefinedFault:{messageFault.Code.IsPredefinedFault}{Environment.NewLine}";
            toReturn +=
                $"\t\tCreateMessageFault().Code.IsReceiverFault:{messageFault.Code.IsReceiverFault}{Environment.NewLine}";
            toReturn +=
                $"\t\tCreateMessageFault().Code.IsSenderFault:{messageFault.Code.IsSenderFault}{Environment.NewLine}";
            toReturn += $"\t\tCreateMessageFault().Code.Name:{messageFault.Code.Name}{Environment.NewLine}";
            toReturn += $"\t\tCreateMessageFault().Code.Namespace:{messageFault.Code.Namespace}{Environment.NewLine}";
            toReturn += $"\t\tCreateMessageFault().Code.SubCode:{messageFault.Code.SubCode}{Environment.NewLine}";


            toReturn += $"\tCreateMessageFault().HasDetail:{messageFault.HasDetail}{Environment.NewLine}";
            toReturn += $"\tCreateMessageFault().Node:{messageFault.Node}{Environment.NewLine}";
            toReturn += $"\tCreateMessageFault().Reason:{messageFault.Reason}{Environment.NewLine}";
            toReturn += $"\tCreateMessageFault().Reason:{messageFault.Reason}{Environment.NewLine}";
            toReturn += $"\tCreateMessageFault().Reason:{messageFault.Reason}{Environment.NewLine}";

            toReturn += $"\tDetail:{GetDetail(messageFault)}{Environment.NewLine}";
        }
        catch (Exception)
        {

            toReturn += $"Exception.CreateMessageFault() - Failed{Environment.NewLine}";
        }

        toReturn += Environment.NewLine;
        toReturn +=
            $"InnerException:{(faultException.InnerException != null ? ExceptionHelper.ExceptionToListOfInnerMessages(faultException.InnerException) : "Null")}";

        return toReturn;
    }

    private static string GetDetail(MessageFault messageFault)
    {
        if (!messageFault.HasDetail) return "NULL";
        if (!new[] { EnvelopeVersion.None, EnvelopeVersion.Soap11, EnvelopeVersion.Soap12 }.Any()) return "NULL";
        return messageFault.GetDetail<string>();
    }
}