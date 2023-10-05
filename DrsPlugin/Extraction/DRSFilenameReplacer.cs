using Rdmp.Core.QueryBuilding;
using System;
using System.Collections.Generic;
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

    public string GetCorrectFilename(DataRow originalRow, string[] columns, Nullable<int> index)
    {
        if (_extractionIdentifier is null)
        {
            throw new Exception("No Extraction Identifier configured");
        }
        string correctFileName = (string)originalRow[_extractionIdentifier.GetRuntimeName()];
        var dt = new DateTimeTypeDecider(new CultureInfo("en-GB"));

        foreach (var column in columns)
        {
            string cellValue = originalRow[column].ToString();
            try
            {
                //try and parse each value into a date, will fail if there is no valid date found
                var date = (DateTime)dt.Parse(cellValue.ToString());
                correctFileName = $"{correctFileName}_{date:yyyy-MM-dd}";
                continue;
            }
            catch (FormatException)
            {
                correctFileName = $"{correctFileName}_{cellValue}";
            }
            catch (Exception)
            {
                //do nothing as the string must be empty
            }
        }
        var ext = Path.GetExtension(originalRow[_filenameColumnName].ToString());

        if (index is not null)
        {
            correctFileName = $"{correctFileName}_{index}{ext}";
        }
        else
        {
            correctFileName = $"{correctFileName}{ext}";

        }
        //filename will be in the format {ReleaseId}_{ _ seperated column list values}_{index}.{extention}
        //this was traditionally {ReleaseId}_{Examination_Date}_{Image_Num}.{ext}
        return correctFileName;
    }
}