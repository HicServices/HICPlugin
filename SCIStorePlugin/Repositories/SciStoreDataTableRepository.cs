using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories
{
    public class SciStoreDataTableRepository : IRepository<SciStoreReport>
    {
        private readonly IDataTableSchemaSource _dataTableSchemaSource;
        private const string HeaderTableName = "Header";
        private const string SampleDetailsTableName = "SampleDetails";
        private const string ResultsTableName = "Results";

        public DataTable HeadersTable { get; private set; }
        public DataTable SampleDetailsTable { get; private set; }
        public DataTable ResultsTable { get; private set; }

        public SciStoreDataTableRepository(IDataTableSchemaSource dataTableSchemaSource)
        {
            _dataTableSchemaSource = dataTableSchemaSource;
        }

        public IEnumerable<SciStoreReport> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SciStoreReport> ReadSince(DateTime day)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<SciStoreReport>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
        {
            throw new NotImplementedException();
        }

        private void CreateDataTables()
        {
            HeadersTable = CreateDataTable(HeaderTableName);

            // Need to give HeadersTable primary keys
            var pkColumns = new [] { HeadersTable.Columns["LabNumber"], HeadersTable.Columns["TestReportID"] };
            HeadersTable.PrimaryKey = pkColumns;
            HeadersTable.CaseSensitive = false;

            SampleDetailsTable = CreateDataTable(SampleDetailsTableName);

            // Need to give HeadersTable primary keys
            pkColumns = new[] { SampleDetailsTable.Columns["LabNumber"], SampleDetailsTable.Columns["TestReportID"], SampleDetailsTable.Columns["TestIdentifier"] };
            SampleDetailsTable.PrimaryKey = pkColumns;
            SampleDetailsTable.CaseSensitive = false;

            ResultsTable = CreateDataTable(ResultsTableName);
        }

        private DataTable CreateDataTable(string headerTableName)
        {
            var dataTable = new DataTable(headerTableName);
            _dataTableSchemaSource.SetSchema(dataTable);
            return dataTable;
        }

        public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
        {
            // Read the reports into the set of data tables
            CreateDataTables();

            var headerColumns = HeadersTable.Columns;
            foreach (var report in reports)
            {
                CreateRow(report.Header, typeof (SciStoreHeader), HeadersTable, headerColumns, listener);

                var sampleDetailsColumns = SampleDetailsTable.Columns;
                foreach (var sample in report.Samples)
                {
                    CreateRow(sample, typeof(SciStoreSample), SampleDetailsTable, sampleDetailsColumns, listener);
                    CreateRows(sample.Results, ResultsTable, listener);
                }
            }
        }

        private void CreateRows<T>(IEnumerable<T> items, DataTable dataTable, IDataLoadEventListener job)
        {
            var columns = dataTable.Columns;
            var itemType = typeof (T);

            foreach (var item in items)
                CreateRow(item, itemType, dataTable, columns, job);
        }

        private void CreateRow<T>(T item, Type itemType, DataTable dataTable, DataColumnCollection columns, IDataLoadEventListener job)
        {
            var row = dataTable.NewRow();
            foreach (DataColumn col in columns)
            {
                var prop = itemType.GetProperty(col.ColumnName);
                if (prop == null)
                {
                    job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Column '" + col.ColumnName + "' in " + dataTable.TableName + " does not have a corresponding property in the " + itemType.FullName + " object"));
                    throw new Exception(
                        "There is a mismatch between the target database table schema and the SciStoreHeader object. The Data Table is expecting the following columns: " +
                        string.Join(",", columns.Cast<DataColumn>().Select(column => column.ColumnName)));
                }

                row[col.ColumnName] = prop.GetValue(item) ?? DBNull.Value;
            }

            try
            {
                dataTable.Rows.Add(row);
            }
            catch (ConstraintException e)
            {
                // find the existing row
                var queryParts = new List<string>();
                foreach (DataColumn column in dataTable.PrimaryKey)
                    queryParts.Add(column.ColumnName + " = '" + row[column.ColumnName] + "'");
                
                var existing = dataTable.Select(string.Join(" AND ", queryParts)).Single();
                var message = "Data Table already contains: " + Environment.NewLine +
                              DataRowToStringWithoutCHI(existing) + Environment.NewLine +
                              Environment.NewLine + "**REPLACING WITH **: " + Environment.NewLine +
                              DataRowToStringWithoutCHI(row);

                dataTable.Rows.Remove(existing);
                dataTable.Rows.Add(row);

                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, message, e));
            }
        }

        private string DataRowToStringWithoutCHI(DataRow row)
        {
            var dataString = "";
            foreach (DataColumn column in row.Table.Columns)
            {
                if (column.ColumnName == "CHI") continue;
                dataString += column.ColumnName + ": " + row[column.ColumnName] + Environment.NewLine;
            }

            return dataString;
        }

        public event InsertionErrorHandler InsertionError;
        protected virtual void OnInsertionError(SqlException exception, string querystring)
        {
            var handler = InsertionError;
            if (handler != null) handler(this, exception, querystring);
        }
    }
}