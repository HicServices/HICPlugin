using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using CatalogueLibrary.DataFlowPipeline;
using HICPlugin.DataFlowComponents.ColumnSwapping;
using NUnit.Framework;
using Plugin.Test;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace HICPluginTests.Integration
{
    [TestFixture]
    public class ColumnSwapTests : DatabaseTests
    {
        const string MappingDb = "TestColumnSwapping";

        protected Exception SetupException;

        #region Tests Which Throw
        [Test]
        [ExpectedException(ExpectedMessage = "There are 0 rules configured")]
        public void NoRulesConfigured_Throws()
        {
            if (SetupException != null)
                throw SetupException;

            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Check(new ThrowImmediatelyCheckNotifier());
        }

        [Test]
        [ExpectedException(ExpectedMessage = "Found rule with missing(blank) Left or Right Operand : ([LocalInput]|)")]
        public void RuleMissingOperand_Throws()
        {

            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[1];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]","");

            swapper.Check(new ThrowImmediatelyCheckNotifier());
        }

        [Test]
        [ExpectedException(ExpectedMessage = "Rules array contains null elements!")]
        public void RulesArrayInitializedButButWithNullsOnly_Throws()
        {

            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[100];

            swapper.Check(new ThrowImmediatelyCheckNotifier());
        }
        [Test]
        [ExpectedException(MatchType = MessageMatch.Contains, ExpectedMessage = "OneToManyErrors:1")]
        public void OneToManyError_Throws()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[1];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]", "[MapInput]");

            swapper.Check(new ThrowImmediatelyCheckNotifier());

            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Fish", "Thomas"); //"Fish" alone maps to both 1 and 4

            swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
        }

        [Test]
        [ExpectedException(MatchType = MessageMatch.Contains, ExpectedMessage = "OneToZeroErrors:1")]
        public void OneToZeroError_Throws()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[1];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]", "[MapInput]");

            swapper.Check(new ThrowImmediatelyCheckNotifier());

            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Imaginariam", "Thomas"); //"Fish" alone maps to both 1 and 4

            swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
        }

        [Test]
        [ExpectedException(MatchType = MessageMatch.Contains, ExpectedMessage = "ManyToOneErrors:2")]
        public void ManyToOneError_Throws()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[2];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]", "[MapInput]");
            swapper.Configuration.Rules[1] = new SubstitutionRule("[Caller]", "[Caller]");

            swapper.Check(new ThrowImmediatelyCheckNotifier());

            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Fish", "Frank"); //"Fish" alone maps to both 1 and 4
            dt.Rows.Add("Soap", "Frank"); //"Fish" alone maps to both 1 and 4

            swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
        }

        #endregion

        #region Tests That Pass
        [Test]
        public void TestColumnSwappingTwoRules_Passes()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[2];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]", "[MapInput]");
            swapper.Configuration.Rules[1] = new SubstitutionRule("[Caller]", "[Caller]");

            swapper.Check(new ThrowImmediatelyCheckNotifier());

            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Fish", "Thomas"); //"Fish" alone maps to both 1 and 4 but Thomas included resolves this by mapping to 1

            var result = swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.AreEqual(typeof(int), result.Columns["MapOutput"].DataType);
            Assert.AreEqual(1, result.Rows[0]["MapOutput"]);
            Assert.AreEqual(1, result.Rows.Count);
        }
        [Test]
        public void TestColumnSwappingOneRule_Passes()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[1];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]","[MapInput]");
            
            swapper.Check(new ThrowImmediatelyCheckNotifier());
            
            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Ball", "Thomas"); //ball has 1 to 1 mappign with int value 2
            dt.Rows.Add("Spade", "Thomas"); //spade has 1 to 1 mapping with int value 3

            var result = swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
         
   
            Assert.AreEqual(typeof(int),result.Columns["MapOutput"].DataType);
            Assert.AreEqual(2, result.Rows[0]["MapOutput"]);
            Assert.AreEqual(3, result.Rows[1]["MapOutput"]);
            Assert.AreEqual(2,result.Rows.Count );
        }

        [Test]
        public void NameTheSameBeforeAndAfter()
        {
            ColumnSwapper swapper = new ColumnSwapper();
            swapper.Configuration = new ColumnSwapConfiguration();

            swapper.Configuration.Server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.Name;
            swapper.Configuration.Database = MappingDb;
            swapper.Configuration.MappingTableName = "MyMap";

            swapper.Configuration.ColumnToPerformSubstitutionOn = "LocalInput";
            swapper.Configuration.SubstituteColumn = "MapOutput";

            swapper.Configuration.Rules = new SubstitutionRule[1];
            swapper.Configuration.Rules[0] = new SubstitutionRule("[LocalInput]", "[MapInput]");

            swapper.Check(new ThrowImmediatelyCheckNotifier());

            DataTable dt = new DataTable();
            dt.Columns.Add("LocalInput");
            dt.Columns.Add("Caller");

            dt.Rows.Add("Ball", "Thomas"); //ball has 1 to 1 mappign with int value 2
            dt.Rows.Add("Spade", "Thomas"); //spade has 1 to 1 mapping with int value 3
            dt.TableName = "Fish";

            var result = swapper.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
         
            Assert.AreEqual("Fish",result.TableName);
        }
        #endregion

        [TestFixtureSetUp]
        public void Setup()
        {
            SetupException = null;

            try
            {
                var server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn;

                //if it wasn't cleaned up properly last time
                if (server.ExpectDatabase(MappingDb).Exists())
                    TearDown();
                    
                server.CreateDatabase(MappingDb);

                using (var con = new SqlConnection(server.Builder.ConnectionString))
                {
                    con.Open();
                    con.ChangeDatabase(MappingDb);

                    SqlCommand cmdCreateTestMap = new SqlCommand("CREATE TABLE MyMap(MapInput varchar(10),Caller varchar(10), MapOutput int)",con);
                    cmdCreateTestMap.ExecuteNonQuery();

                    SqlCommand cmdInsert = new SqlCommand("INSERT INTO MyMap VALUES ('Fish','Thomas',1)",con);
                    cmdInsert.ExecuteNonQuery();

                    cmdInsert = new SqlCommand("INSERT INTO MyMap VALUES ('Ball','Thomas',2)",con);
                    cmdInsert.ExecuteNonQuery();

                    cmdInsert = new SqlCommand("INSERT INTO MyMap VALUES ('Spade','Thomas',3)",con);
                    cmdInsert.ExecuteNonQuery();

                    cmdInsert = new SqlCommand("INSERT INTO MyMap VALUES ('Fish','Frank',4)",con);
                    cmdInsert.ExecuteNonQuery();
                
                    cmdInsert = new SqlCommand("INSERT INTO MyMap VALUES ('Soap','Frank',4)", con);
                    cmdInsert.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                SetupException = e;
            }
            
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(MappingDb).ForceDrop();
        }
    }
}
