using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using DataLoadEngine.Mutilators;
using NUnit.Framework;

namespace HICPluginTests.Integration
{
    public class CHIPopulatorTests : PluginDatabaseTests
    {
        [Test]
        public void FindExportedClassFromCatalogue()
        {
            string dllFile = Directory.EnumerateFiles(".", "HICPlugin.dll").SingleOrDefault();
            if (dllFile == null)
                Assert.Inconclusive("Could not find the file HICPlugin.dll in " + new DirectoryInfo(".").FullName);

            LoadModuleAssembly.CreateNewLoadModuleAssembly(new FileInfo(dllFile),true );
            
            Dictionary<string, Exception> badAssemblies = LoadModuleAssembly.ListBadAssemblies();

            if (badAssemblies.ContainsKey(dllFile))
                throw badAssemblies[dllFile];

            Assert.NotNull(LoadModuleAssembly.FactoryCreateA<IMutilateDataTables>("LoadModules.Specific.HIC.CHIPopulatorAnywhere")); 
        }
    }
}