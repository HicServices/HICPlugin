#nullable enable
using Rdmp.Core.QueryBuilding;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using TypeGuesser.Deciders;

namespace DrsPlugin.Extraction;

public sealed class DRSFilenameReplacer
{
    private static readonly DateTimeTypeDecider Dt = new (new CultureInfo("en-GB"));

    private readonly IColumn _extractionIdentifier;
    private readonly string _filenameColumnName;

    public DRSFilenameReplacer(IColumn extractionIdentifier, string filenameColumnName)
    {
        ArgumentNullException.ThrowIfNull(extractionIdentifier);
        _extractionIdentifier = extractionIdentifier;
        _filenameColumnName = filenameColumnName;
    }


    /// <summary>
    /// Insert substitutions in the filename, converting date values to yyyy-MM-dd format and optionally adding a counter
    /// </summary>
    /// <param name="originalRow"></param>
    /// <param name="columns"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string GetCorrectFilename(DataRow originalRow, IEnumerable<string> columns, int? index)
    {
        var correctFileName = (string)originalRow[_extractionIdentifier.GetRuntimeName()];

        foreach (var cellValue in columns.Select(column => originalRow[column].ToString()))
        {
            try
            {
                //try and parse each value into a date, will fail if there is no valid date found
                var date = (DateTime)Dt.Parse(cellValue);
                correctFileName = $"{correctFileName}_{date:yyyy-MM-dd}";
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

        correctFileName = index is not null ? $"{correctFileName}_{index}{ext}" : $"{correctFileName}{ext}";

        //filename will be in the format {ReleaseId}_{ _ separated column list values}_{index}.{extension}
        //this was traditionally {ReleaseId}_{Examination_Date}_{Image_Num}.{ext}
        return correctFileName;
    }
}