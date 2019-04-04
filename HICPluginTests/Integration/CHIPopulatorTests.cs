using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataLoadEngine.Mutilators;
using NUnit.Framework;

namespace HICPluginTests.Integration
{
    public class CHIPopulatorTests
    {
        [Test]
        public void FindExportedClassFromCatalogue()
        {
            var repo = new MemoryCatalogueRepository();

            //find the current plugin dll (ourselves)
            string dllFile = Directory.EnumerateFiles(TestContext.CurrentContext.TestDirectory, "HICPlugin.dll").SingleOrDefault();
            if (dllFile == null)
                Assert.Inconclusive("Could not find the file HICPlugin.dll in " + new DirectoryInfo(".").FullName);

            //upload it to the repo
            var plugin = new CatalogueLibrary.Data.Plugin(repo, new FileInfo("Fish.zip"));

            try
            {
                //declare a lma
                var lma = new LoadModuleAssembly(repo, new FileInfo(dllFile), plugin);

                //setup MEF to load the current directory
                var mef = new MEF();
                mef.Setup(new SafeDirectoryCatalog(TestContext.CurrentContext.TestDirectory));

                //ensure HICPlugin is not deemed to be a bad plugin
                Dictionary<string, Exception> badAssemblies = mef.ListBadAssemblies();

                if (badAssemblies.ContainsKey(dllFile))
                    throw badAssemblies[dllFile];

                //create our instance using MEF.
                Assert.NotNull(mef.FactoryCreateA<IMutilateDataTables>("HICPlugin.CHIPopulatorAnywhere"));
            }
            finally
            {
                plugin.DeleteInDatabase();
            }
        }
    }
}