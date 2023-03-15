using System;
using System.Data;
using Microsoft.Data.SqlClient;
using FAnsi.Discovery;

namespace SCIStorePlugin.Repositories;

public class DataTableSchemaFromDatabase : IDataTableSchemaSource
{
    private readonly DiscoveredDatabase _destinationDatabase;

    public DataTableSchemaFromDatabase(DiscoveredDatabase destinationDatabase)
    {
        _destinationDatabase = destinationDatabase;
    }

    public void SetSchema(DataTable dataTable)
    {
        if (string.IsNullOrWhiteSpace(dataTable.TableName))
            throw new Exception("The DataTable must have a TableName in order for its schema to be set this way");

        var cmdText = $"SELECT TOP 0 * FROM [{dataTable.TableName}]";
            
        var server = _destinationDatabase.Server;
        server.ChangeDatabase(_destinationDatabase.GetRuntimeName());

        using (var con = server.GetConnection())
        {
            using (var sqlAdapter = new SqlDataAdapter(cmdText, (SqlConnection)con))
            {
                sqlAdapter.Fill(dataTable);
            }
        }
    }
}