﻿using System;
using System.Data.SqlClient;
using NUnit.Framework;
using SCIStorePlugin.Repositories;
using Tests.Common;

namespace SCIStorePluginTests.Integration
{
    public class ReflectionToDatabaseTester : DatabaseTests
    {
        class TestObject
        {
            public string Field1 { get; set; }
        }
        
        [Test]
        public void SendValidDomainObject()
        {
            TestObject t = new TestObject();
            t.Field1 = "1q234fj";

            var dbInfo = DiscoveredDatabaseICanCreateRandomTablesIn;

            using (var con = (SqlConnection)dbInfo.Server.GetConnection())
            {
                con.Open();

                SqlCommand cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
                cmdDrop.ExecuteNonQuery();

                SqlCommand cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
                cmdCreateTestTable.ExecuteNonQuery();

                ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute<TestObject>(t, con, dbInfo, "TestObject");
            }
        }


        [Test]
        public void SendTooLongFieldToDatabase()
        {
            TestObject t = new TestObject();
            t.Field1 = "asdljkmalsdjflaksdjflkajsd;lfkjasdl;kfj";

            var dbInfo = DiscoveredDatabaseICanCreateRandomTablesIn;
            using (var con = (SqlConnection)dbInfo.Server.GetConnection())
            {
                con.Open();

                SqlCommand cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
                cmdDrop.ExecuteNonQuery();

                SqlCommand cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
                cmdCreateTestTable.ExecuteNonQuery();

                try
                {
                    var ex = Assert.Throws<Exception>(() => ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute<TestObject>(t, con, dbInfo, "TestObject"));
                    Assert.IsTrue(ex.Message.Contains("Field1 in table TestObject is defined as length  10 in the database but you tried to insert a string value of length 41"));
                }
                finally
                {
                    new SqlCommand("DROP TABLE TestObject", con).ExecuteNonQuery();
                }
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
            TestObject2 t = new TestObject2();
            t.Field1 = "asdljfj";
            t.Field2 = null;

            var dbInfo = DiscoveredDatabaseICanCreateRandomTablesIn;
            using (var con = (SqlConnection)dbInfo.Server.GetConnection())
            {
                con.Open();
                SqlCommand cmdDrop = new SqlCommand("IF OBJECT_ID('dbo.TestObject', 'U') IS NOT NULL DROP TABLE dbo.TestObject", con);
                cmdDrop.ExecuteNonQuery();

                SqlCommand cmdCreateTestTable = new SqlCommand("CREATE TABLE TestObject ( Field1 varchar(10))", con);
                cmdCreateTestTable.ExecuteNonQuery();

                try
                {
                    var ex = Assert.Throws<Exception>(() => ReflectionBasedSqlDatabaseInserter.MakeInsertSqlAndExecute(t, con, dbInfo, "TestObject"));
                    Assert.IsTrue(ex.Message.Contains("Domain object has a property called Field2 which does not exist in table TestObject"));
                }
                finally
                {
                    new SqlCommand("DROP TABLE TestObject", con).ExecuteNonQuery();
                }
            }
        }
    }
}