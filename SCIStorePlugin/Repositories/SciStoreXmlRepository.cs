using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class SciStoreXmlRepository : IRepository<SciStoreReport>
{
    private readonly string _rootPath;

    public SciStoreXmlRepository(string rootPath)
    {
        _rootPath = rootPath;
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

    public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
    {
        var serialiser = new XmlSerializer(typeof (SciStoreReport));
        foreach (var report in reports)
        {
            if (report == null)
                throw new Exception("Could not cast SciStoreReport object");

            try
            {
                var path = $"{_rootPath}{Path.DirectorySeparatorChar}report-{report.Header.LabNumber}.xml";
                using (var stream = new StreamWriter(path, false))
                {
                    serialiser.Serialize(stream, report);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open stream to write CombinedReportData files: {e}");
            }
        }
    }
}