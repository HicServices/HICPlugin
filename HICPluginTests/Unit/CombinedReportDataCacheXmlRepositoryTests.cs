using NUnit.Framework;
using Rdmp.Core.Caching.Layouts;
using ReusableLibraryCode.Progress;
using SCIStorePlugin;
using SCIStorePlugin.Cache;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HICPluginTests.Unit
{
    internal class CombinedReportDataCacheXmlRepositoryTests
    {
        [Test]
        public void TestPath()
        {
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "bleh"));
            dir.Create();

            var layout = new SCIStoreCacheLayout( dir, 
                new SCIStoreLoadCachePathResolver(HealthBoard.T, Discipline.Biochemistry));

            var repo = new CombinedReportDataCacheXmlRepository(layout);
            repo.Create(new[] { new CombinedReportData{
            HbExtract = "T",
            SciStoreRecord = new SciStoreRecord
            {
                LabNumber = "sdfdsf",
                TestReportID = "fff",
            }}},new ThrowImmediatelyDataLoadEventListener());
        }
    }
}
