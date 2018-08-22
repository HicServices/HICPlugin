using System;
using System.Text;
using CatalogueLibrary.Data;
using CatalogueLibrary.QueryBuilding;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Sources;
using ReusableLibraryCode.DatabaseHelpers.Discovery.QuerySyntax;
using ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents
{
    public class ChrisHallSpecialExplicitSource : ExecuteDatasetExtractionSource
    {
        [DemandsInitialization("The database you want a using statement put infront of")]
        public string DatabaseToUse { get; set; }
        
        [DemandsInitialization("The collation you want injected into join SQL")]
        public string Collation { get; set; }

        public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
        { 
            StringBuilder sb = new StringBuilder();

            if(!string.IsNullOrWhiteSpace(Collation))
                ((QueryBuilder) Request.QueryBuilder).AddCustomLine("collate " + Collation, QueryComponent.JoinInfoJoin);

            if (!string.IsNullOrWhiteSpace(DatabaseToUse))
                sb.AppendLine("USE " + DatabaseToUse);

            sb.AppendLine(Request.QueryBuilder.SQL);

            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "HACKED SQL" + Environment.NewLine + "------------------------------------------" + Environment.NewLine + sb));

            return sb.ToString();
        }
    }
}
