using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Sources;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    public class ChrisHallSpecialExplicitSource : ExecuteDatasetExtractionSource
    {
        [DemandsInitialization("The database you want a using statement put infront of")]
        public string DatabaseToUse { get; set; }

        public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
        {
            return "USE " + DatabaseToUse + Environment.NewLine + Environment.NewLine + base.HackExtractionSQL(sql, listener);


        }
    }
}
