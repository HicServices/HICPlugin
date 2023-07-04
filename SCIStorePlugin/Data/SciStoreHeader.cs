using System;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

/// <summary>
/// Data Transfer Object
/// (Database column names should really be named according to the same C# conventions...)
/// </summary>
public class SciStoreHeader
{
    private string _labNumber;
    private string _testReportId;
    public string CHI { get; set; }

    public string LabNumber
    {
        get { return _labNumber; }
        set { _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
    }

    public string TestReportID
    {
        get { return _testReportId; }
        set { _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value); }
    }

    public string PatientID { get; set; }
    public string ClinicalDataRequired { get; set; } // ServiceRequest > ClinicalDataRequired

    // ServiceProvider > ProvidingLocation
    public string ProvidingOrganisationID { get; set; }
    public string ProvidingOrganisationName { get; set; }
    public string ProvidingOrganisationType { get; set; }

    // RequestingParty
    public string RequestingPartyID { get; set; }
    public string RequestingPartyPosition { get; set; }
    public string RequestingPartyName { get; set; }
    public string RequestingPartyStatus { get; set; }

    // RequestingParty > EmployingOrganisation
    public string RequestingOrganisationID { get; set; } // Source_code in old schema
    public string RequestingOrganisationName { get; set; }
    public string RequestingOrganisationStatus { get; set; }
    public string RequestingOrganisationType { get; set; }

    public string Discipline { get; set; }
    public string hb_extract { get; set; }
}
    
public static class SciStoreHeaderFactory 
{
    public static SciStoreHeader Create(CombinedReportData combinedReport)
    {
        var combinedReportHeader = combinedReport.SciStoreRecord;

        // todo: agree on removal of this check - we don't use ReportType for anything, and it is not recorded in the database
        // if (!combinedReportHeader.ReportType.Equals(combinedReportHeader.LabNumber))
        //    throw new SciStoreDodgyXmlException("Expected LabNumber field to have same as ReportType but ReportType was " + combinedReportHeader.ReportType, combinedReportHeader.LabNumber);
            
        var report = combinedReport.InvestigationReport;
        var reportData = report.ReportData;

        var header = new SciStoreHeader
        {
            LabNumber = CleanLabNumber(combinedReport),
            TestReportID = combinedReportHeader.TestReportID,
            PatientID = combinedReportHeader.patientid,
            Discipline = reportData.Discipline,
            CHI = CleanCHI(combinedReportHeader.CHI),
            hb_extract = combinedReport.HbExtract,
                
            //ClinicalDataRequired is ok to be null? TN
            ClinicalDataRequired = reportData.ServiceRequest.ClinicalDataRequired == null ? null : string.Join(" ", reportData.ServiceRequest.ClinicalDataRequired)
        };

        PopulateConsultantInfo(header, reportData.RequestingParty);
            
        if (reportData.ServiceProvider != null && reportData.ServiceProvider.ProvidingLocation != null)
        {
            var providingLocation = reportData.ServiceProvider.ProvidingLocation;
            header.ProvidingOrganisationID = providingLocation.OrganisationId.IdValue;
            header.ProvidingOrganisationName = providingLocation.OrganisationName;
            header.ProvidingOrganisationType = providingLocation.OrganisationType;
        }

        if (reportData.RequestingParty != null && reportData.RequestingParty.EmployingOrganisation != null)
        {
            var org = reportData.RequestingParty.EmployingOrganisation;
            header.RequestingOrganisationName = org.OrganisationName;
            header.RequestingOrganisationType = org.OrganisationType;

            var id = org.OrganisationId;
            if (id != null)
            {
                header.RequestingOrganisationID = id.IdValue;
                header.RequestingOrganisationStatus = id.Status.Status;
            }
        }

        return header;
    }

    private static string CleanCHI(string chi)
    {
        // encountered in Fife Haematology load
        if (!string.IsNullOrWhiteSpace(chi) && chi.Equals("Temp Residen"))
            return chi[..10];

        return chi;
    }

    private static string CleanLabNumber(CombinedReportData combinedReport)
    {
        var combinedReportHeader = combinedReport.SciStoreRecord;
        var report = combinedReport.InvestigationReport;
        var reportData = report.ReportData;

        if (reportData.Discipline.Equals("Biochemistry") 
            && !combinedReportHeader.LabNumber.StartsWith("C")
            && !combinedReportHeader.LabNumber.StartsWith("POC"))
            throw new Exception($"Malformed (or otherwise unexpected) LabNumber: {combinedReportHeader.LabNumber}");

        var labNumber = combinedReportHeader.LabNumber;
            
        if (labNumber.Length <= 10) return labNumber;
            
        if (labNumber.StartsWith("Merge")) // found in Tayside Haematology
        {
            labNumber = labNumber.Remove(0, "Merge".Length);
            if (labNumber.Length > 10) // *still* greather than 10 characters
                throw new Exception(
                    $"LabNumber contains Merge and is longer than 10 characters even after removal of 'Merge': {combinedReportHeader.LabNumber}");
        }

        return labNumber;
    }

    private static void PopulateConsultantInfo(SciStoreHeader header, HCP_DETAIL_TYPE requestingParty)
    {
        ID_TYPE consultant = null;

        if (requestingParty == null)
            return;

        if (!requestingParty.HcpId.Any(id => id.IdScheme.Equals("Requestor")))
            throw new SciStoreDodgyXmlException("Missing HcpId of Scheme Requestor", header.LabNumber);

        var dodgyIDScheme = requestingParty.HcpId.FirstOrDefault(id => !id.IdScheme.Equals("Requestor") && !id.IdScheme.Equals("SCISTOREINPUT"));
        if (dodgyIDScheme != null)
            throw new SciStoreDodgyXmlException($"Unexpected HcpId=>IdScheme {dodgyIDScheme.IdScheme}", header.LabNumber);

        foreach (var requestor in requestingParty.HcpId)
        {
            if (requestor.IdScheme.Equals("SCISTOREINPUT"))
                continue;

            if (consultant == null)
                consultant = requestor;
            else
            if (consultant != requestor)
                throw new SciStoreDodgyXmlException(
                    $"Multiple different requestors {consultant.IdValue} AND {requestor.IdValue}", header.LabNumber);
        }

        if (consultant != null)
        {
            header.RequestingPartyID = consultant.IdValue;
            header.RequestingPartyPosition = requestingParty.Position;
            header.RequestingPartyStatus = consultant.Status.Status;
        }

        var personalName = requestingParty.HcpName;
        if (personalName != null)
        {
            if (personalName.Item is STRUCTURED_NAME_TYPE structuredName)
            {
                header.RequestingPartyName =
                    $"{structuredName.Title} {structuredName.GivenName} {structuredName.MiddleName} {structuredName.FamilyName}";
                return;
            }

            if (personalName.Item is string unstructuredName)
            {
                header.RequestingPartyName = unstructuredName;
                return;
            }

            throw new Exception(
                $"Unsupported type of item (PERSONAL_NAME_TYPE.Item) encountered in Lab {header.LabNumber}/{header.TestReportID}. Type is {personalName.GetType().FullName}");
        }

    }

}