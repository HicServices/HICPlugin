using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Repositories;
using DataLoadEngine.Mutilators;
using HICPlugin;
using NUnit.Framework;
using Plugin.Test;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using Tests.Common;
using Plugin = CatalogueLibrary.Data.Plugin;

namespace HICPluginTests.Integration
{
    public class CHIPopulatorTests : DatabaseTests
    {
        [Test]
        public void FindExportedClassFromCatalogue()
        {
            string dllFile = Directory.EnumerateFiles(".", "HICPlugin.dll").SingleOrDefault();
            if (dllFile == null)
                Assert.Inconclusive("Could not find the file HICPlugin.dll in " + new DirectoryInfo(".").FullName);

            var remnant = CatalogueRepository.GetAllObjects<CatalogueLibrary.Data.Plugin>().SingleOrDefault(p => p.Name.Equals("Fish.zip"));
            if (remnant != null)
                remnant.DeleteInDatabase();


            var plugin = new CatalogueLibrary.Data.Plugin(CatalogueRepository, new FileInfo("Fish.zip"));
            try
            {
                var lma = new LoadModuleAssembly(CatalogueRepository, new FileInfo(dllFile), plugin);
            
                Dictionary<string, Exception> badAssemblies = CatalogueRepository.MEF.ListBadAssemblies();

                if (badAssemblies.ContainsKey(dllFile))
                    throw badAssemblies[dllFile];

                Assert.NotNull(CatalogueRepository.MEF.FactoryCreateA<IMutilateDataTables>("HICPlugin.CHIPopulatorAnywhere"));
            }
            finally
            {
                plugin.DeleteInDatabase();
            }
        }
    }
}