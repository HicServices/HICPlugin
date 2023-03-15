using System.Data;

namespace SCIStorePlugin.Repositories;

public interface IDataTableSchemaSource
{
    void SetSchema(DataTable dataTable);
}