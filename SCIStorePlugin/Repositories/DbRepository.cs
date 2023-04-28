using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using NLog;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Repositories;

public abstract class DbRepository<T> : IRepository<T> where T : class, new()
{
    protected readonly DatabaseHelper DatabaseHelper;
    protected readonly SciStoreTableRecord TableInfo;

    public abstract IEnumerable<T> ReadAll();
    public abstract IEnumerable<T> ReadSince(DateTime day);
    public IEnumerable<IEnumerable<T>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public abstract void Create(IEnumerable<T> reports, IDataLoadEventListener listener);

    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    protected DbRepository(DatabaseHelper databaseHelper, SciStoreTableRecord tableInfo)
    {
        DatabaseHelper = databaseHelper;
        TableInfo = tableInfo;
    }

    protected T DoFindQuery(string query)
    {
        var command = DatabaseHelper.CreateCommand(query);
        command.Connection.Open();
        var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        var findObject = reader.HasRows ? Hydrate(reader) : null;
        command.Connection.Close();
        return findObject;
    }

    protected T Hydrate(SqlDataReader reader)
    {
        var toHydrate = new T();
        var properties = typeof(T).GetProperties();

        reader.Read();
        foreach (var property in properties)
        {
            property.SetValue(toHydrate, reader[property.Name] is DBNull ? null : reader[property.Name], null);
        }
        return toHydrate;
    }

    protected int Create(T testCodeLookup, string tableName, string idColumnName)
    {
        var sql = ReflectionBasedSqlDatabaseInserter.MakeInsertSql(testCodeLookup, DatabaseHelper.Database, tableName, idColumnName);
        try
        {
            var result = DatabaseHelper.ExecuteScalarObject(sql);
            return Convert.ToInt32(result);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Could not insert DoFindQuery into {TableInfo.DatabaseName}.{TableInfo.TestCodesTable}: {e}");
        }
    }

}