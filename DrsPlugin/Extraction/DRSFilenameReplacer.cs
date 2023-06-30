using Rdmp.Core.QueryBuilding;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using TypeGuesser.Deciders;

namespace DrsPlugin.Extraction;

public class DRSFilenameReplacer
{
    private readonly IColumn _extractionIdentifier;
    private readonly string _filenameColumnName;

    public DRSFilenameReplacer(IColumn extractionIdentifier, string filenameColumnName)
    {
        _extractionIdentifier = extractionIdentifier;
        _filenameColumnName = filenameColumnName;
    }

    public string GetCorrectFilename(DataRow originalRow, IDataLoadEventListener _)
    {
        //DRS files are always in uk format?
        var dt = new DateTimeTypeDecider(new CultureInfo("en-GB"));

        return
            $"{originalRow[_extractionIdentifier.GetRuntimeName()]}_{((DateTime)dt.Parse(originalRow["Examination_Date"].ToString())):yyyy-MM-dd}_{originalRow["Eye"]}M_{originalRow["Image_Num"]}_PW{originalRow["Pixel_Width"]}_PH{originalRow["Pixel_Height"]}{Path.GetExtension(originalRow[_filenameColumnName].ToString())}";
    }
}