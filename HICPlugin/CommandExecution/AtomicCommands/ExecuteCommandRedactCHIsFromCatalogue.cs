using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandExecution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rdmp.Core.Curation.Data;
using YamlDotNet.Serialization;
using System.IO;
using HICPluginInteractive.DataFlowComponents;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using System.Data;
using static NPOI.HSSF.Util.HSSFColor;
using Rdmp.Core.Curation.Data.Defaults;
using TB.ComponentModel;
using FAnsi.Discovery.TableCreation;
using HICPlugin.Curation.Data;

namespace HICPlugin.CommandExecution.AtomicCommands;

public class ExecuteCommandRedactCHIsFromCatalogue : BasicCommandExecution, IAtomicCommand
{

    private Catalogue _catalouge;
    private IBasicActivateItems _activator;
    private bool _bailOutEarly;
    private readonly Dictionary<string, List<string>> _allowLists = new();


    public ExecuteCommandRedactCHIsFromCatalogue(IBasicActivateItems activator, [DemandsInitialization("The catalogue to search")] Catalogue catalogue, string allowListLocation = null) : base(activator)
    {
        _catalouge = catalogue;
        _activator = activator;
        if (allowListLocation != null)
        {
            var allowListFileContent = File.ReadAllText(allowListLocation);
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<Dictionary<string, List<string>>>(allowListFileContent);
            foreach (var (cat, columns) in yamlObject)
            {
                _allowLists.Add(cat, columns);
            }
        }
    }

    //private DataTable GetRedactionDataTable()
    //{
        //var c = _activator.RepositoryLocator.CatalogueRepository.
            //var dqe = _activator.ServerDefaults.GetDefaultFor(PermissableDefaults.DQE);
        //var discoveredDQE = dqe.Discover(DataAccessContext.InternalDataProcessing);
        //var table = discoveredDQE.DiscoverTables(false).Where(t => t.GetRuntimeName() == "dbo.RedactedCHIs");
        //if (table != null)
        //{
        //    return table.ToDataTable();
        //}
        //var args = new CreateTableArgs(discoveredDQE, "dbo.RedactedCHIs","");
        //table = (IEnumerable<FAnsi.Discovery.DiscoveredTable>)discoveredDQE.CreateTable(args);
        //Console.WriteLine(table);
        //Console.WriteLine(table.ToString());
        //return table.ToDataTable();

    //}

    private void handleFoundCHI(string foundChi, string table, string column, string columnValue)
    {
        Console.WriteLine("Found CHI!");
        //var rc = new RedactedCHI() { PotentialCHI = foundChi, CHIContext="testing" };
        //rc.SaveToDatabase();
        var redactedValue = columnValue.Replace(foundChi, "REDACTED");//TODO save to db and get ID back
        var sql = $"UPDATE {table} SET {column}='{redactedValue}' where {column}='{columnValue}'";
        var server = _catalouge.GetDistinctLiveDatabaseServer(DataAccessContext.InternalDataProcessing, false);
        var conn = server.GetConnection();
        conn.Open();
        using (var cmd = server.GetCommand(sql, conn))
        {
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }


    public override void Execute()
    {
        base.Execute();
        List<string> columnAllowList = new();
        if (_allowLists.TryGetValue("RDMP_ALL", out var _extractionSpecificAllowances))
            columnAllowList.AddRange(_extractionSpecificAllowances);
        if (_allowLists.TryGetValue(_catalouge.Name, out var _catalogueSpecificAllowances))
            columnAllowList.AddRange(_catalogueSpecificAllowances.ToList());
        foreach (var item in _catalouge.CatalogueItems)
        {
            if (columnAllowList.Contains(item.Name)) continue;

            var column = item.ColumnInfo.Name;
            int idxOfLastSplit = column.LastIndexOf('.');
            string table = column[..idxOfLastSplit];
            var columnName = column[(idxOfLastSplit + 1)..];
            var server = _catalouge.GetDistinctLiveDatabaseServer(DataAccessContext.InternalDataProcessing, false);
            var sql = $"SELECT {columnName} from {table}";
            var dt = new DataTable();
            dt.BeginLoadData();
            using (var cmd = server.GetCommand(sql, server.GetConnection()))
            {
                using var da = server.GetDataAdapter(cmd);
                da.Fill(dt);
            }
            dt.EndLoadData();
            foreach (DataRow row in dt.Rows)
            {

                var value = row[dt.Columns[0].ColumnName].ToString();
                var potentialCHI = CHIColumnFinder.GetPotentialCHI(value);
                if (!string.IsNullOrWhiteSpace(potentialCHI))
                {
                    handleFoundCHI(potentialCHI, table,columnName,value);
                }
            }
        }
    }
}
