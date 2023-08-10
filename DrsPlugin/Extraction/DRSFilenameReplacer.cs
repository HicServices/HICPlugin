using Rdmp.Core.QueryBuilding;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using TypeGuesser.Deciders;

namespace DrsPlugin.Extraction;

public sealed class DRSFilenameReplacer
{
    private readonly IColumn _extractionIdentifier;
    private readonly string _filenameColumnName;

    public DRSFilenameReplacer(IColumn extractionIdentifier, string filenameColumnName)
    {
        _extractionIdentifier = extractionIdentifier;
        _filenameColumnName = filenameColumnName;
    }

    public string GetCorrectFilename(DataRow originalRow)
    {
        //DRS files are always in uk format?
        var dt = new DateTimeTypeDecider(new CultureInfo("en-GB"));
        var id = originalRow[_extractionIdentifier.GetRuntimeName()];
        var date = (DateTime)dt.Parse(originalRow["Examination_Date"].ToString());
        var num = originalRow["Image_Num"];
        var ext = Path.GetExtension(originalRow[_filenameColumnName].ToString());

        return
            $"{id}_{date:yyyy-MM-dd}_{num}{ext}";
    }
}