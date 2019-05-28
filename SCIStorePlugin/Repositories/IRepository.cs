using System.Collections.Generic;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories
{
    public interface IRepository<T>
    {
        IEnumerable<T> ReadAll();
        void Create(IEnumerable<T> reports, IDataLoadEventListener listener);
    }

    public delegate void AfterReadAllHandler();
    public delegate void AfterReadSingleHandler(object sender, CombinedReportData report);
}
