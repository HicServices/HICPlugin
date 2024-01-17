using System;
using Microsoft.Data.SqlClient;
using FAnsi;
using NUnit.Framework;
using SCIStorePlugin.Repositories;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

public class ReflectionToDatabaseTester : DatabaseTests
{
    class TestObject
    {
        public string Field1 { get; set; }
    }

    [Test]
    public void SendValidDomainObject()
    {
        var t = new TestObject
        {
            Field1 = "1q234fj"
        };

        var dbInfo = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

        using var con = (SqlConnection)dbInfo.Server.GetConnection();
        con.Open();

        using var cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
        cmdDrop.ExecuteNonQuery();

        using var cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
        cmdCreateTestTable.ExecuteNonQuery();

        ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute<TestObject>(t, con, dbInfo, "TestObject");
    }


    [Test]
    public void SendTooLongFieldToDatabase()
    {
        var t = new TestObject
        {
            Field1 = "asdljkmalsdjflaksdjflkajsd;lfkjasdl;kfj"
        };

        var dbInfo = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        using var con = (SqlConnection)dbInfo.Server.GetConnection();
        con.Open();

        var cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
        cmdDrop.ExecuteNonQuery();

        var cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
        cmdCreateTestTable.ExecuteNonQuery();

        try
        {
            var ex = Assert.Throws<Exception>(() => ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute<TestObject>(t, con, dbInfo, "TestObject"));
            Assert.That(ex?.Message.Contains("Field1 in table TestObject is defined as length  10 in the database but you tried to insert a string value of length 41"),Is.True);
        }
        finally
        {
            using var q=new SqlCommand("DROP TABLE TestObject", con);
            q.ExecuteNonQuery();
        }
    }

    class TestObject2
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }

    [Test]
    public void SendNonExistantColumn()
    {
        var t = new TestObject2
        {
            Field1 = "asdljfj",
            Field2 = null
        };

        var dbInfo = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        using var con = (SqlConnection)dbInfo.Server.GetConnection();
        con.Open();
        using var cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
        cmdDrop.ExecuteNonQuery();

        using var cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
        cmdCreateTestTable.ExecuteNonQuery();

        try
        {
            var ex = Assert.Throws<Exception>(() => ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute(t, con, dbInfo, "TestObject"));
            Assert.That(ex?.Message.Contains("Domain object has a property called Field2 which does not exist in table TestObject"),Is.True);
        }
        finally
        {
            using var q=new SqlCommand("DROP TABLE TestObject", con);
            q.ExecuteNonQuery();
        }
    }
}