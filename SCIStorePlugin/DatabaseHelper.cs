using System.Data;
using Microsoft.Data.SqlClient;

namespace SCIStorePlugin;

public class DatabaseHelper
{
    public string Database { get; set; }

    public string Server { get; private set; }

    public string Username { get; }

    public string Password { get; }

    private readonly int _timeout;
    private readonly bool _integratedSecurity;
    private readonly SqlConnectionStringBuilder _builder;

    // TODO: Construction needs refactored

    public DatabaseHelper(string server, string database, int timeout = 30)
    {
        Server = server;
        Database = database;
        _timeout = timeout;
        _integratedSecurity = true;
    }

    public DatabaseHelper(string server, string database, string username, string password, int timeout = 30)
    {
        Server = server;
        Database = database;
        _timeout = timeout;
        _integratedSecurity = false;
        Username = username;
        Password = password;
    }

    public DatabaseHelper(SqlConnectionStringBuilder builder)
    {
        _builder = builder;
        Database = _builder.InitialCatalog;
    }

    public string ConnectionString()
    {
        if (_builder != null)
            return _builder.ConnectionString;

        var sb = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = _integratedSecurity
        };

        if (!_integratedSecurity)
        {
            sb.UserID = Username;
            sb.Password = Password;
        }

        return sb.ConnectionString;
    }

    public SqlCommand CreateCommand(string sql)
    {
        return new SqlCommand
        {
            Connection = new SqlConnection(ConnectionString()),
            CommandText = sql,
            CommandTimeout = _timeout
        };
    }

    public SqlCommand CreateCommand(SqlConnection conn, string sql)
    {
        return new SqlCommand
        {
            Connection = conn,
            CommandText = sql,
            CommandTimeout = _timeout
        };
    }

    public SqlCommand CreateStoredProcedure(string sp)
    {
        return new SqlCommand(sp)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _timeout
        };
    }

    public object ExecuteScalarObject(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteScalar();
    }

    public int ExecuteNonQuery(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteNonQuery();
    }

    public SqlDataReader ExecuteReader(string sql)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        var command = CreateCommand(conn, sql);
        return command.ExecuteReader();
    }

    public int ExecuteNonQueryCommand(SqlCommand cmd)
    {
        using var conn = new SqlConnection(ConnectionString());
        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    public DataTable GetDataTableFor(string tableName)
    {
        using var cmd = CreateCommand($"SELECT TOP 0 * FROM {tableName}");
        var dt = new DataTable(tableName);
        var da = new SqlDataAdapter
        {
            SelectCommand = cmd,
            MissingSchemaAction = MissingSchemaAction.AddWithKey
        };
                
        da.Fill(dt);

        return dt;
    }
}