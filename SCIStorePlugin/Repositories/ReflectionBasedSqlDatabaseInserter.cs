using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using MapsDirectlyToDatabaseTable;
using FAnsi.Discovery;

namespace SCIStorePlugin.Repositories
{
    public class ReflectionBasedSqlDatabaseInserter
    {
        /// <summary>
        /// This handles null objects, and makes a string suitable for SQL
        /// we handle strings, and DateTime
        /// </summary>
        /// <param name="o"></param>
        /// <param name="param">this goes inside the ToString() method call</param>
        /// <returns></returns>
        public static string MakeString(object o, string param = null)
        {
            var s = o as string;
            if (s != null)
                return Quote(CleanForSql(s));

            if (o is DateTime)
                return Quote(((DateTime)o).ToString("yyyy-MM-dd HH:mm:ss"));

            return "NULL";
        }

        public static object MakeValue(object o)
        {
            if (o == null)
                return "NULL";

            if (o is string || o is DateTime)
                return MakeString(o);

            if (o is bool)
                return (bool)o ? 1 : 0;

            return o;
        }

        private static string CleanForSql(string notSafe)
        {
            return notSafe.Replace("'", " ");
        }

        [Pure]
        public static string Quote(string str, string surround = "'")
        {
            return surround + str + surround;
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties<T>(string idColumnName)
        {


            Type type = typeof(T);
            if (type.IsPrimitive || type.Equals(typeof(string)))
            {
                // simple case ...
            }

            //todo improve performance of this (only do it once , not once per record)
            return typeof(T).GetProperties()
                .Where(
                        info =>
                            info.Name != idColumnName &&
                            (
                                !Attribute.IsDefined(info, typeof(NoMappingToDatabase))
                            )
                );
        }

        [Pure]
        public static string MakeInsertCollectionSql<T>(IEnumerable<T> results, string databaseName, string tableName, string idColumnName = null)
        {
            var properties = GetMappableProperties<T>(idColumnName).ToList();
            var resultColumnNames = properties.Select(info => info.Name);
            var valueStrings = new List<string>();
            foreach (var result in results)
            {
                var resultValues = properties.Select(info => MakeValue(info.GetValue(result, null)));
                valueStrings.Add("(" + String.Join(",", resultValues) + ")");
            }

            return String.Format("INSERT INTO {0}..{1} ({2}) VALUES {3}",
                databaseName, tableName, String.Join(",", resultColumnNames), String.Join(",", valueStrings));
        }

        [Pure]
        public static string MakeInsertSql<T>(T header, string databaseName, string tableName,
            string idColumnName = null)
        {
            var properties = GetMappableProperties<T>(idColumnName).ToList();
            var columnNames = properties.Select(info => info.Name);
            var values = properties.Select(info => MakeValue(info.GetValue(header, null)));

            return String.Format("INSERT INTO {0}..{1} ({2}) VALUES ({3}) SELECT SCOPE_IDENTITY()",
                databaseName, tableName, String.Join(",", columnNames), String.Join(",", values));
        }

        public static int MakeInsertSqlAndExecute<T>(T reflectObject, SqlConnection con, DiscoveredDatabase dbInfo, string tableName, string idColumnName = null)
        {
            var properties = GetMappableProperties<T>(idColumnName).ToList();
            var columnNames = properties.Select(info => info.Name);
            var values = properties.Select(info => MakeValue(info.GetValue(reflectObject, null))).ToArray();

            string sql = String.Format("INSERT INTO [{0}]..{1} ({2}) VALUES ({3}) SELECT SCOPE_IDENTITY()",
                dbInfo.GetRuntimeName(), tableName, String.Join(",", columnNames), String.Join(",", values));



            try
            {
                SqlCommand cmdInsert = new SqlCommand(sql, con);
                return cmdInsert.ExecuteNonQuery();

            }
            catch (SqlException e)
            {
                ThrowBetterException<T>(reflectObject, tableName, properties, values, dbInfo, e);
                throw;
            }
        }

        private static void ThrowBetterException<T>(T reflectObject, string tableName, List<PropertyInfo> properties, object[] values, DiscoveredDatabase dbInfo, SqlException originalExcetion)
        {
            string problemsDetected = "";

            Dictionary<string, object> reflectedObjectDictionary = new Dictionary<string, object>();

            for (int i = 0; i < properties.Count; i++)
                reflectedObjectDictionary.Add(properties[i].Name, values[i]);


            var listColumns = dbInfo.ExpectTable(tableName).DiscoverColumns();

            try
            {
                foreach (DiscoveredColumn column in listColumns)
                {
                    if (!reflectedObjectDictionary.ContainsKey(column.GetRuntimeName()))
                        problemsDetected += "Column " + column + " exists in database table " + tableName +
                                            " but does not exist on domain object " + typeof(T).FullName +
                                            Environment.NewLine;
                    else
                    {
                        object valueInDomainObject = reflectedObjectDictionary[column.GetRuntimeName()];

                        if (valueInDomainObject == null)
                            continue;
                        else
                        {
                            string s = valueInDomainObject as string;
                            if (s != null)
                            {
                                int lengthInDatabase = column.DataType.GetLengthIfString();

                                if (lengthInDatabase < s.Length)
                                    problemsDetected +=
                                        "Column " + column + " in table " + tableName + " is defined as length  " + lengthInDatabase + " in the database but you tried to insert a string value of length " + s.Length + Environment.NewLine;
                            }
                        }
                    }

                    foreach (PropertyInfo property in properties)
                        if (!listColumns.Any(c => c.GetRuntimeName().Equals(property.Name)))
                            problemsDetected += "Domain object has a property called " + property.Name +
                                                " which does not exist in table " + tableName + Environment.NewLine;
                }
            }
            catch (Exception)
            {
                //something went wrong building a better exception so just throw original one
                throw originalExcetion;
            }

            if (!string.IsNullOrWhiteSpace(problemsDetected))
            {
                string toThrow = "Original Message:" + originalExcetion.Message + Environment.NewLine;
                toThrow += "We Detected Problems:" + Environment.NewLine;
                toThrow += problemsDetected;
                throw new Exception(toThrow, originalExcetion);
            }

            throw originalExcetion;
        }
    }
}