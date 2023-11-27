using HICPluginInteractive.DataFlowComponents;
using Microsoft.Data.SqlClient;
using NPOI.HSSF.Record;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace HICPlugin.CommandExecution.AtomicCommands;

public class ExecuteCommandIdentifyCHIInCatalogue : BasicCommandExecution, IAtomicCommand
{

    private Catalogue _catalouge;
    private IBasicActivateItems _activator;
    private bool _bailOutEarly;
    private readonly Dictionary<string, List<string>> _allowLists = new();


    public ExecuteCommandIdentifyCHIInCatalogue(IBasicActivateItems activator, [DemandsInitialization("The catalogue to search")] Catalogue catalogue, bool bailOutEarly = false, string allowListLocation = null) : base(activator)
    {
        _catalouge = catalogue;
        _activator = activator;
        _bailOutEarly = bailOutEarly;
        if(allowListLocation != null)
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



    private void handleFoundCHI(string foundChi,string contextValue, string columnName)
    {
        if(foundChis.Rows.Count == 0)
        {
            //init
            foundChis.Columns.Add("Potential CHI");
            foundChis.Columns.Add("Context");
            foundChis.Columns.Add("Source Column Name");
        }
        var foundIndex = contextValue.IndexOf(foundChi);
        var shrunkContext = $"{contextValue[Math.Max(0,foundIndex-25)..foundIndex]}{foundChi}{contextValue[(foundIndex+foundChi.Length)..Math.Min(foundIndex + foundChi.Length+25,contextValue.Length)]}";
        //var shrunkContext = contextValue;//$"{contextValue.Substring(Math.Max(0, foundIndex - 25), foundIndex)}{foundChi}{contextValue.Substring(foundIndex+foundChi.Length, foundIndex + foundChi.Length+25)}";
        foundChis.Rows.Add(foundChi, shrunkContext, columnName);
    }
    private DataTable foundChis = new();

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

            if (_bailOutEarly && foundChis.Rows.Count > 0)
            {
                break;
            }
            var column = item.ColumnInfo.Name;
            int idxOfLastSplit = column.LastIndexOf('.');
            var columnName = column[(idxOfLastSplit + 1)..];
            var server = _catalouge.GetDistinctLiveDatabaseServer(DataAccessContext.InternalDataProcessing, false);
            var sql = $"SELECT {columnName} from {column[..idxOfLastSplit]}";
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
                    handleFoundCHI(potentialCHI, value, item.Name);
                    if (_bailOutEarly)
                    {
                        break;
                    }
                }
                

            }
        }
        Console.WriteLine($"Found {foundChis.Rows.Count} CHIs in the {_catalouge.Name} Catalogue.");
        foreach(DataRow row in foundChis.Rows)
        {
            Console.WriteLine($"{row["potential CHI"]} | {row["Context"]} | {row["Source Column Name"]}");

        }
    }
}
