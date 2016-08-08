using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using DataLoadEngine.Mutilators;
using NUnit.Framework;
using Plugin.Test;
using Plugin = CatalogueLibrary.Data.Plugin;

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

            new LoadModuleAssembly(CatalogueRepository, new FileInfo(dllFile), new CatalogueLibrary.Data.Plugin(CatalogueRepository,new FileInfo("Fish.zip")));
            
            Dictionary<string, Exception> badAssemblies = CatalogueRepository.MEF.ListBadAssemblies();

            if (badAssemblies.ContainsKey(dllFile))
                throw badAssemblies[dllFile];

            Assert.NotNull(CatalogueRepository.MEF.FactoryCreateA<IMutilateDataTables>("HICPlugin.CHIPopulatorAnywhere")); 
        }
    }
}