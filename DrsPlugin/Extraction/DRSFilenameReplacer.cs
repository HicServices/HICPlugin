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

    private Nullable<DateTime> isDate(string cellValue)
    {
        DateTime dateTime;
        string[] formats = {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                   "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                   "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                   "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                   "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};//todo use a better list, this was yanked from the internet
        if (DateTime.TryParseExact(cellValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
        {
            return dateTime;
        }
        return null;
    }

    public string GetCorrectFilename(DataRow originalRow, string[] columns, Nullable<int> index)
    {
        if (_extractionIdentifier is null)
        {
            throw new Exception("No Extraction Identifier configured");
        }
        string correctFileName = (string)originalRow[_extractionIdentifier.GetRuntimeName()];
        var dt = new DateTimeTypeDecider(CultureInfo.InvariantCulture);

        //Loops over the list of passed in columns
        foreach (var column in columns)
        {
            // if(!originalRow.IsNull(column)){
            //     throw new Exception($"Column {column} doesn't exist!");
            // }
            string cellValue = originalRow[column].ToString();
            var dateTimeConversion = isDate(cellValue);
            if (dateTimeConversion is not null)
            {
                correctFileName = $"{correctFileName}_{dateTimeConversion:yyyy-MM-dd}";
                continue;
            }
            correctFileName = $"{correctFileName}_{cellValue}";
        }
        var ext = Path.GetExtension(originalRow[_filenameColumnName].ToString());

        if (index is not null)
        {
            correctFileName = $"{correctFileName}_{index}{ext}";
        }
        else
        {
            correctFileName = $"{correctFileName}_{ext}";

        }
        //filename will be in the format {ReleaseId}_{ _ seperated column list values}_{index}.{extention}
        //this was traditionally {ReleaseId}_{examination_date}_{image_num}.{ext}

        return correctFileName;
    }
}