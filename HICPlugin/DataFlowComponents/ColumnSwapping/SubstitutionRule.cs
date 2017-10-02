using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ReusableLibraryCode.DatabaseHelpers.Discovery;

namespace HICPlugin.DataFlowComponents.ColumnSwapping
{
    public class SubstitutionRule
    {
        //required for XML serialization
        public SubstitutionRule()
        {
            
        }
        public SubstitutionRule(string leftOperand, string rightOperand)
        {
            LeftOperand = leftOperand;
            RightOperand = rightOperand;
        }

        public string LeftOperand { get; set; }
        public string RightOperand { get; set; }

        public SubstitutionResult CheckRule(List<SubstitutionRule> priorRules, string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable, SqlConnection conToSourceTable, int timeout, out string sql)
        {
            if (conToSourceTable.State != ConnectionState.Open)
                throw new Exception("Connection was not open, you must open it before you pass it to this method");

            SubstitutionResult toReturn;

            string forwardSql;
            toReturn = DetectForwardMappingErrors(priorRules, sqlOriginTable, sqlMappingTable, substituteInSourceColumn, substituteForInMappingTable, conToSourceTable, timeout, out forwardSql);


            string reverseSql;
            int additionalMTo1Errors = DetectReversabilityMappingErrors(priorRules, sqlOriginTable, sqlMappingTable, substituteInSourceColumn, substituteForInMappingTable, conToSourceTable, timeout, out reverseSql);


            toReturn.ManyToOneErrors += additionalMTo1Errors;
            


            sql =
                "--Forward mapping error detection " + Environment.NewLine +
                forwardSql + Environment.NewLine +
                "--Reverse mapping error detection " + Environment.NewLine +
                reverseSql;

            return toReturn;

        }

        #region Forwards Checking code
        private SubstitutionResult DetectForwardMappingErrors(List<SubstitutionRule> priorRules, string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable, SqlConnection conToSourceTable, int timeout, out string sql)
        {
            
            string andStatement = "";

            //add the prior rules too
            foreach (SubstitutionRule priorRule in priorRules)
                andStatement += Environment.NewLine + priorRule.GetWhereSql() + Environment.NewLine + " AND ";

            andStatement += Environment.NewLine + GetWhereSql();


            sql =
                string.Format(@"
select
CountOfDistinctMappingIdentifiers,
count(*) as NumberOfRowsInSourceWithThisManyMappings
from
(

    SELECT 
        (SELECT count(Distinct {0}) from {1} map where {2} ) as CountOfDistinctMappingIdentifiers 
    FROM  {3} source
) results
group by
results.CountOfDistinctMappingIdentifiers
order by
results.CountOfDistinctMappingIdentifiers asc
",

                    substituteForInMappingTable,
                    sqlMappingTable,
                    andStatement,
                    sqlOriginTable);

            SubstitutionResult toReturn;
            try
            {
                toReturn = ExecuteAndProcessSqlToFindForwardErrors(sql, conToSourceTable, timeout);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to send SQL :" + Environment.NewLine + sql, e);
            }

            return toReturn;
        }

        public class SubstitutionResult
        {
            public int OneToZeroErrors { get; set; }
            public int OneToOneSucceses { get; set; }
            public int OneToManyErrors { get; set; }
            public int ManyToOneErrors { get; set; }

            public override string ToString()
            {
                return string.Format(
                    @"
OneToZeroErrors:{0}
OneToOneSucceses:{1}
OneToManyErrors:{2}
ManyToOneErrors:{3}
",
                    OneToZeroErrors,
                    OneToOneSucceses,
                    OneToManyErrors,
                    ManyToOneErrors);
            }

            public bool IsExactlyOneToOne(bool allowMto1Errors, bool allow1ToZeroErrors)
            {
                return
                    (OneToZeroErrors == 0 || allow1ToZeroErrors) && 
                    OneToManyErrors == 0 && 
                    (ManyToOneErrors == 0 || allowMto1Errors);
            }
        }

        private SubstitutionResult ExecuteAndProcessSqlToFindForwardErrors(string sql, SqlConnection con, int timeout)
        {
            var toReturn = new SubstitutionResult();

            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.CommandTimeout = timeout;
            var r = cmd.ExecuteReader();

            if (!r.HasRows)
                throw new Exception("Failed to get any results when executing the following SQL:" + Environment.NewLine
                                    + sql);
            
            while (r.Read())
            {
                int countOfDistinctMappingIdentifiers = Convert.ToInt32(r["CountOfDistinctMappingIdentifiers"]);

                //bad - no match found
                if (countOfDistinctMappingIdentifiers == 0)
                {
                    toReturn.OneToZeroErrors = Convert.ToInt32(r["NumberOfRowsInSourceWithThisManyMappings"]);
                }
                //good
                if (countOfDistinctMappingIdentifiers == 1)
                    toReturn.OneToOneSucceses = Convert.ToInt32(r["NumberOfRowsInSourceWithThisManyMappings"]);

                //bad - too many matches found = ambiguity
                if (countOfDistinctMappingIdentifiers > 1)
                {
                    toReturn.OneToManyErrors += Convert.ToInt32(r["NumberOfRowsInSourceWithThisManyMappings"]);
                }
            }
            r.Close();


            return toReturn;
        }
        #endregion


        #region Reversability Checking Code

        private int DetectReversabilityMappingErrors(List<SubstitutionRule> priorRules, string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable, SqlConnection conToSourceTable, int timeout, out string sql)
        {
            string andStatement = "";

            //add the prior rules too
            foreach (SubstitutionRule priorRule in priorRules)
                andStatement += Environment.NewLine + priorRule.GetWhereSql() + Environment.NewLine + " AND ";

            andStatement += Environment.NewLine + GetWhereSql();


            sql =
                string.Format(@"		
		select sum(Lost.CountOfIdentifiersLost)
		from
		(
			SELECT 
			count(distinct source.{0}) CountOfIdentifiersLost
			FROM
			{1} source
			join 
			{2} map
			on
			{3}
			group by 
			map.{4}
			having count(distinct source.{0})>1
		)
		Lost
",

                    substituteInSourceColumn,
                    sqlOriginTable,
                    sqlMappingTable,
                    andStatement,
                    substituteForInMappingTable);

            SqlCommand cmd = new SqlCommand(sql, conToSourceTable);
            cmd.CommandTimeout = timeout;
            int lostRows;
            try
            {
                object result = cmd.ExecuteScalar();

                if (result == DBNull.Value)//null means join produced no rows including the having >1 which translates to 0 i.e. no lost records
                {
                    return 0;
                }

                lostRows = Convert.ToInt32(result);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to send SQL :" + Environment.NewLine + sql, e);
            }
            
            return lostRows;
        }


        public string GetWhereSql()
        {

            string source = LeftOperand;
            //if it is a column then alias it
            if (source.StartsWith("["))
                source = "source." + source;

            string destination = RightOperand;
            destination = "map." + destination;

            return source + "=" + destination;
        }
        #endregion

        public static SubstitutionResult CheckRules(DiscoveredTable tempTable, SubstitutionRule[] rules, string sqlOriginTable, string sqlMappingTable, string substituteInSourceColumn, string substituteForInMappingTable , int timeout)
        {
            if (rules == null || rules.Length == 0)
                return null;
            
            string whoCares;

            var server = tempTable.Database.Server;
            using (var con = (SqlConnection)server.GetConnection())
            {
                con.Open();


                //there is only one rule return that one
                if (rules.Length == 1)
                    return rules[0].CheckRule(Enumerable.Empty<SubstitutionRule>().ToList(), sqlOriginTable,
                        sqlMappingTable, substituteInSourceColumn, substituteForInMappingTable, con, timeout, out whoCares);


                //there are more than 1 rules, get all but last:
                List<SubstitutionRule> priorRules = new List<SubstitutionRule>();
                for (int i = 0; i < rules.Length - 1; i++)
                    priorRules.Add(rules[i]);

                //call Check on last element but listing the others as priors (currently order is actually irrelevant so we could use rules[0] and rules.Skip(1) but in future order might matter
                return rules.Last().CheckRule(priorRules, sqlOriginTable,
                        sqlMappingTable, substituteInSourceColumn, substituteForInMappingTable, con, timeout, out whoCares);

            }
        }
    }
}