using FAnsi.Discovery;
using MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Curation.Data.ImportExport;
using Rdmp.Core.Curation.Data.Serialization;
using Rdmp.Core.DataLoad.Modules.Attachers;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;
using ReusableUIComponents.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace GoDartsPluginUI.CommandExecution.AtomicCommands
{
    public class ExecuteCommandSetupGoFusionFromDatabase : BasicUICommandExecution, IAtomicCommand
    {
        private IExternalDatabaseServer _loggingServer;

        public ExecuteCommandSetupGoFusionFromDatabase(IActivateItems activator) : base(activator)
        {
            _loggingServer = Activator.ServerDefaults.GetDefaultFor(PermissableDefaults.LiveLoggingServer_ID);

            if (_loggingServer == null)
                SetImpossible("There is no default logging server configured");
        }

        public override void Execute()
        {
            base.Execute();

            var db = SelectDatabase("Import all Tables form Database...");

            ShareManager shareManager = new ShareManager(Activator.RepositoryLocator, LocalReferenceGetter);

            List<ICatalogue> cataloguesToMatch = new List<ICatalogue>();
            List<ICatalogue> importedCatalogues = new List<ICatalogue>();

            //don't do any double importing!
            var existing = Activator.RepositoryLocator.CatalogueRepository.GetAllObjects<TableInfo>();
            var ignoredTables = new List<TableInfo>();

            if (MessageBox.Show("Would you also like to import ShareDefinitions (metadata)?", "Import Metadata From File(s)", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                OpenFileDialog ofd = new OpenFileDialog() { Multiselect = true };
                ofd.Filter = "Share Definitions|*.sd";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in ofd.FileNames)
                        using (var stream = File.Open(f, FileMode.Open))
                        {
                            var newObjects = shareManager.ImportSharedObject(stream);

                            if (newObjects != null)
                                cataloguesToMatch.AddRange(newObjects.OfType<ICatalogue>());
                        }
                }
            }

            bool generateCatalogues = false;

            if (MessageBox.Show("Would you like to try to guess non-matching Catalogues by Name?", "Guess by name", MessageBoxButtons.YesNo) == DialogResult.Yes)
                cataloguesToMatch.AddRange(Activator.RepositoryLocator.CatalogueRepository.GetAllObjects<Catalogue>());
            else if (MessageBox.Show("Would you like to generate empty Catalogues for non-matching tables instead?", "Generate New Catalogues", MessageBoxButtons.YesNo) == DialogResult.Yes)
                generateCatalogues = true;

            var married = new Dictionary<CatalogueItem, ColumnInfo>();

            TableInfo anyNewTable = null;

            foreach (DiscoveredTable discoveredTable in db.DiscoverTables(includeViews: false))
            {
                var collide = existing.FirstOrDefault(t => t.Is(discoveredTable));
                if (collide != null)
                {
                    ignoredTables.Add(collide);
                    continue;
                }

                var importer = new TableInfoImporter(Activator.RepositoryLocator.CatalogueRepository, discoveredTable);
                TableInfo ti;
                ColumnInfo[] cis;

                //import the table
                importer.DoImport(out ti, out cis);

                anyNewTable = anyNewTable ?? ti;

                //find a Catalogue of the same name (possibly imported from Share Definition)
                var matchingCatalogues = cataloguesToMatch.Where(c => c.Name.Equals(ti.GetRuntimeName(), StringComparison.CurrentCultureIgnoreCase)).ToArray();

                //if theres 1 Catalogue with the same name
                if (matchingCatalogues.Length == 1)
                {
                    importedCatalogues.Add(matchingCatalogues[0]);

                    //we know we want to import all these ColumnInfos
                    var unmatched = new List<ColumnInfo>(cis);

                    //But hopefully most already have orphan CatalogueItems we can hook them together to
                    foreach (var cataItem in matchingCatalogues[0].CatalogueItems)
                        if (cataItem.ColumnInfo_ID == null)
                        {
                            var matches = cataItem.GuessAssociatedColumn(cis, allowPartial: false).ToArray();

                            if (matches.Length == 1)
                            {
                                cataItem.SetColumnInfo(matches[0]);
                                unmatched.Remove(matches[0]); //we married them together
                                married.Add(cataItem, matches[0]);
                            }
                        }

                    //is anyone unmarried? i.e. new ColumnInfos that don't have CatalogueItems with the same name
                    foreach (ColumnInfo columnInfo in unmatched)
                    {
                        var cataItem = new CatalogueItem(Activator.RepositoryLocator.CatalogueRepository, (Catalogue)matchingCatalogues[0], columnInfo.GetRuntimeName());
                        cataItem.ColumnInfo_ID = columnInfo.ID;
                        cataItem.SaveToDatabase();
                        married.Add(cataItem, columnInfo);
                    }
                }
                else if (generateCatalogues)
                {
                    Catalogue newCatalogue;
                    CatalogueItem[] discardCi;
                    ExtractionInformation[] discardEi;
                    new ForwardEngineerCatalogue(ti, cis).ExecuteForwardEngineering(out newCatalogue, out discardCi, out discardEi);
                    importedCatalogues.Add(newCatalogue);
                }
            }

            if (married.Any() && MessageBox.Show("Found " + married.Count + " columns, make them all extractable?", "Make Extractable", MessageBoxButtons.YesNo) == DialogResult.Yes)
                foreach (var kvp in married)
                {
                    //yup thats how we roll, the database is main memory!
                    var ei = new ExtractionInformation(Activator.RepositoryLocator.CatalogueRepository, kvp.Key, kvp.Value, kvp.Value.Name);
                }

            if (ignoredTables.Any())
                WideMessageBox.Show("Ignored " + ignoredTables.Count + " tables", "Because they already existed as TableInfos:" + string.Join(Environment.NewLine, ignoredTables.Select(ti => ti.GetRuntimeName())));

            var lmd = CreateLoadMetadata(importedCatalogues);

            if (anyNewTable != null)
                Publish(anyNewTable);

            Publish(lmd);
            Emphasise(lmd);
        }

        private LoadMetadata CreateLoadMetadata(List<ICatalogue> importedCatalogues)
        {
            var lmd = new LoadMetadata(Activator.RepositoryLocator.CatalogueRepository);
            lmd.Name = "Load GoDartsFusion";
            lmd.Description = "Load GoDarts Fusion Database from the released MDF from DLS";
            lmd.SaveToDatabase();

            foreach (var catalogue in importedCatalogues)
            {
                ((Catalogue)catalogue).LoadMetadata_ID = lmd.ID;
                catalogue.LoggingDataTask = lmd.Name;
                catalogue.SaveToDatabase();
            }

            var attacher = new ProcessTask(Activator.RepositoryLocator.CatalogueRepository, lmd, LoadStage.Mounting);
            attacher.Name = "Attach MDF";
            attacher.ProcessTaskType = ProcessTaskType.Attacher;
            attacher.Path = typeof (MDFAttacher).FullName;
            attacher.SaveToDatabase();

            var metadataImporter = new ProcessTask(Activator.RepositoryLocator.CatalogueRepository, lmd, LoadStage.PostLoad);
            metadataImporter.Name = "Import Metadata files";
            metadataImporter.ProcessTaskType = ProcessTaskType.DataProvider;
            // HACK: Fix this when the class becomes public!
            metadataImporter.Path = "LoadModules.Generic.DataProvider.ShareDefinitionImporter";
            metadataImporter.SaveToDatabase();

            return lmd;
        }

        private int? LocalReferenceGetter(PropertyInfo property, RelationshipAttribute relationshipattribute, ShareDefinition sharedefinition)
        {
            if (property.Name.EndsWith("LoggingServer_ID"))
                return _loggingServer.ID;


            throw new SharingException("Could not figure out a sensible value to assign to Property " + property);
        }


        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.Database, OverlayKind.Execute);
        }
    }
}