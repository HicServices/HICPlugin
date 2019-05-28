using System;
using System.Collections.Generic;
using ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories
{
    public class SciStoreFileRepository : IRepository<SciStoreReport>
    {
        private readonly string _rootPath;
        private readonly SciStoreTableRecord _tableInfo;

        // TODO: tableInfo doesn't make too much sense in the context of a File Repository - fix by injecting the Sql maker
        public SciStoreFileRepository(string rootPath, SciStoreTableRecord tableInfo)
        {
            _rootPath = rootPath;
            _tableInfo = tableInfo;
        }

        public IEnumerable<SciStoreReport> ReadSince(DateTime day)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<SciStoreReport>> ChunkedReadFromDateRange(DateTime start, DateTime end,IDataLoadEventListener job)
        {
            throw new NotImplementedException();
        }

        public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SciStoreReport> ReadAll()
        {
            throw new NotImplementedException();
        }
    }
}