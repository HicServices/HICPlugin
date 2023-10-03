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

    public string GetCorrectFilename(DataRow originalRow,List<Tuple<string,bool>> columns, int index)
    {
        //DRS files are always in uk format?
        if(_extractionIdentifier is null){
            throw new Exception("No Extraction Identifier configured");
        }
        string correctFileName = (string)originalRow[_extractionIdentifier.GetRuntimeName()];
        var dt = new DateTimeTypeDecider(new CultureInfo("en-GB"));

        //Loops over the list of passed in columns
        foreach (var column in columns){
            String columnName = column.Item1;
            bool isDateTimeColumn = column.Item2;
            if (isDateTimeColumn)
            {
                var date = (DateTime)dt.Parse(originalRow[columnName].ToString());
                correctFileName = $"{correctFileName}_{date:yyyy-MM-dd}";
                continue;
            }
            var columnValue = originalRow[columnName].ToString();
            correctFileName = $"{correctFileName}_{columnValue}";
        }
        //var dt = new DateTimeTypeDecider(new CultureInfo("en-GB"));
        //var id = originalRow[_extractionIdentifier.GetRuntimeName()];
        //var date = (DateTime)dt.Parse(originalRow["Examination_Date"].ToString());
        //var num = originalRow["Image_Num"];
        var ext = Path.GetExtension(originalRow[_filenameColumnName].ToString());
        correctFileName = $"{correctFileName}_{index}{ext}";


        return correctFileName;//$"{id}_{date:yyyy-MM-dd}_{num}{ext}";
    }
}