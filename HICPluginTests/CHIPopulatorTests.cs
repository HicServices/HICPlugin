using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using DataLoadEngine.Mutilators;
using NUnit.Framework;
using Tests.Common;

namespace HICPluginTests
{
    
    public class CHIPopulatorTests : DatabaseTests
    {

        [Test]
        public void FindExportedClassFromCatalogue()
        {
            string dllFile = Directory.EnumerateFiles(".", "HICPlugin.dll").Single();
            LoadModuleAssembly.CreateNewLoadModuleAssembly(new FileInfo(dllFile),true );
            
            Dictionary<string, Exception> badAssemblies = LoadModuleAssembly.ListBadAssemblies();

            if (badAssemblies.ContainsKey(dllFile))
                throw badAssemblies[dllFile];

            Assert.NotNull(LoadModuleAssembly.FactoryCreateA<IMutilateDataTables>("LoadModules.Specific.HIC.CHIPopulatorAnywhere")); 
        }
    }
}