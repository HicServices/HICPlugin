using Rdmp.Core.QueryBuilding;
using ReusableLibraryCode.Progress;
using System;
using System.Data;
using System.IO;

namespace DrsPlugin.Extraction
{
    public class DRSFilenameReplacer
    {
        private readonly IColumn _extractionIdentifier;
        private readonly string _filenameColumnName;

        public DRSFilenameReplacer(IColumn extractionIdentifier, string filenameColumnName)
        {
            _extractionIdentifier = extractionIdentifier;
            _filenameColumnName = filenameColumnName;
        }

        public string GetCorrectFilename(DataRow originalRow, IDataLoadEventListener listener)
        {
            return string.Format("{0}_{1}_{2}M_{3}_PW{4}_PH{5}{6}",
                originalRow[_extractionIdentifier.GetRuntimeName()],
                DateTime.Parse(originalRow["Examination_Date"].ToString()).ToString("yyyy-MM-dd"), 
                originalRow["Eye"], originalRow["Image_Num"],
                originalRow["Pixel_Width"], originalRow["Pixel_Height"],
                Path.GetExtension(originalRow[_filenameColumnName].ToString()));
        }
    }
}