using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace DrsPluginTests
{
    public class ExtractionTests : DatabaseTests
    {
        [Test]
        public void FilenameReplacerTest()
        {
            var dataset = new DataTable("Dataset");
            dataset.Columns.Add("ReleaseID");
            dataset.Columns.Add("Examination_Date");
            dataset.Columns.Add("Eye");
            dataset.Columns.Add("Image_Num");
            dataset.Columns.Add("Pixel_Width");
            dataset.Columns.Add("Pixel_Height");
            dataset.Columns.Add("Image_Filename");
            dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_RM_1_PW1024_PH768.png");

            var extractionIdentifierColumn = MockRepository.GenerateStub<IColumn>();
            extractionIdentifierColumn.Stub(c => c.GetRuntimeName()).Return("ReleaseID");

            var replacer = new DRSFilenameReplacer(extractionIdentifierColumn, "Image_Filename");

            Assert.AreEqual("R00001_2016-05-17_RM_1_PW1024_PH768.png", replacer.GetCorrectFilename(dataset.Rows[0], new ThrowImmediatelyDataLoadEventListener()));
        }

        [Test]
        public void ExtractionTestWithZipArchive()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "1"));
            File.WriteAllText(Path.Combine(tempDir.FullName, "2_P12345_2016-05-07_RM_1_PW1024_PH768.png"), "");

            var archiveDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var archiveSubdir = archiveDir.CreateSubdirectory("1");
            ZipFile.CreateFromDirectory(tempDir.FullName, Path.Combine(archiveSubdir.FullName, "1.zip"), CompressionLevel.Fastest, false);

            var rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            LoadDirectory.CreateDirectoryStructure(rootDir, "DRS");
            var projDir = Path.Combine(rootDir.FullName, "DRS");
            
            var identifierMap = new DataTable("IdentifierMap");
            identifierMap.Columns.Add("PrivateID");
            identifierMap.Columns.Add("ReleaseID");
            identifierMap.Rows.Add("P12345", "R00001");

            var dataset = new DataTable("Dataset");
            dataset.Columns.Add("CHI");
            dataset.Columns.Add("Examination_Date");
            dataset.Columns.Add("Eye");
            dataset.Columns.Add("Image_Num");
            dataset.Columns.Add("Pixel_Width");
            dataset.Columns.Add("Pixel_Height");
            dataset.Columns.Add("Image_Filename");
            dataset.Columns.Add("ImageArchiveUri");
            dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_RM_1_PW1024_PH768.png", @"1\1.zip!2_P12345_2016-05-07_RM_1_PW1024_PH768.png");

            try
            {
                var listener = new ThrowImmediatelyDataLoadEventListener();
                var request = SetupRequestObject(projDir, rootDir, identifierMap, listener);

                var extractionComponent = new DRSImageExtraction
                {
                    DatasetName = new Regex(".*"),
                    FilenameColumnName = "Image_Filename",
                    ImageUriColumnName = "ImageArchiveUri",
                    PathToImageArchive = archiveDir.FullName
                };

                extractionComponent.PreInitialize(request, listener);

                var cts = new GracefulCancellationTokenSource();
                var dt = extractionComponent.ProcessPipelineData(dataset, listener, cts.Token);

                var imageDir = rootDir.EnumerateDirectories("Images").SingleOrDefault();
                Assert.NotNull(imageDir, "Extraction directory to hold images has not been created");

                var imageFiles = imageDir.EnumerateFiles().ToList();
                Assert.AreEqual(1, imageFiles.Count);
                Assert.AreEqual("R00001_2016-05-17_RM_1_PW1024_PH768.png", imageFiles[0].Name);

                Assert.IsFalse(dt.Columns.Contains("ImageArchiveUri"));
            }
            catch (Exception)
            {
                tempDir.Delete(true);
                archiveDir.Delete(true);
                rootDir.Delete(true);
                throw;
            }
        }

        [Test]
        public void ExtractionTestWithTarArchive()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "1"));
            var imagePath = Path.Combine(tempDir.FullName, "2_P12345_2016-05-07_RM_1_PW1024_PH768.png");
            File.WriteAllText(imagePath, "");

            var archiveDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var archiveSubdir = archiveDir.CreateSubdirectory("1");

            using (var fs = File.Create(Path.Combine(archiveSubdir.FullName, "1.tar")))
            {
                using (var archive = TarArchive.CreateOutputTarArchive(fs))
                {
                    var entry = TarEntry.CreateEntryFromFile(imagePath);
                    entry.Name = Path.GetFileName(imagePath);
                    archive.WriteEntry(entry, false);
                }
            }

            var rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            LoadDirectory.CreateDirectoryStructure(rootDir, "DRS");
            var projDir = Path.Combine(rootDir.FullName, "DRS");

            var identifierMap = new DataTable("IdentifierMap");
            identifierMap.Columns.Add("PrivateID");
            identifierMap.Columns.Add("ReleaseID");
            identifierMap.Rows.Add("P12345", "R00001");

            var dataset = new DataTable("Dataset");
            dataset.Columns.Add("CHI");
            dataset.Columns.Add("Examination_Date");
            dataset.Columns.Add("Eye");
            dataset.Columns.Add("Image_Num");
            dataset.Columns.Add("Pixel_Width");
            dataset.Columns.Add("Pixel_Height");
            dataset.Columns.Add("Image_Filename");
            dataset.Columns.Add("ImageArchiveUri");
            dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_RM_1_PW1024_PH768.png", @"1\1.tar!2_P12345_2016-05-07_RM_1_PW1024_PH768.png");

            try
            {
                var listener = new ThrowImmediatelyDataLoadEventListener();
                var request = SetupRequestObject(projDir, rootDir, identifierMap, listener);

                var extractionComponent = new DRSImageExtraction
                {
                    DatasetName = new Regex(".*"),
                    FilenameColumnName = "Image_Filename",
                    ImageUriColumnName = "ImageArchiveUri",
                    PathToImageArchive = archiveDir.FullName
                };

                extractionComponent.PreInitialize(request, listener);

                var cts = new GracefulCancellationTokenSource();
                var dt = extractionComponent.ProcessPipelineData(dataset, listener, cts.Token);

                var imageDir = rootDir.EnumerateDirectories("Images").SingleOrDefault();
                Assert.NotNull(imageDir, "Extraction directory to hold images has not been created");

                var imageFiles = imageDir.EnumerateFiles().ToList();
                Assert.AreEqual(1, imageFiles.Count);
                Assert.AreEqual("R00001_2016-05-17_RM_1_PW1024_PH768.png", imageFiles[0].Name);

                Assert.IsFalse(dt.Columns.Contains("ImageArchiveUri"));
            }
            catch (Exception)
            {
                tempDir.Delete(true);
                archiveDir.Delete(true);
                rootDir.Delete(true);
                throw;
            }
        }

        [Test]
        public void ExtractionTestWithNullImageFilename()
        {
            var rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            LoadDirectory.CreateDirectoryStructure(rootDir, "DRS");
            var projDir = Path.Combine(rootDir.FullName, "DRS");

            var identifierMap = new DataTable("IdentifierMap");
            identifierMap.Columns.Add("PrivateID");
            identifierMap.Columns.Add("ReleaseID");
            identifierMap.Rows.Add("P12345", "R00001");

            var dataset = new DataTable("Dataset");
            dataset.Columns.Add("CHI");
            dataset.Columns.Add("Examination_Date");
            dataset.Columns.Add("Eye");
            dataset.Columns.Add("Image_Num");
            dataset.Columns.Add("Pixel_Width");
            dataset.Columns.Add("Pixel_Height");
            dataset.Columns.Add("Image_Filename");
            dataset.Columns.Add("ImageArchiveUri");
            dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_RM_1_PW1024_PH768.png", null);

            try
            {
                var listener = new ThrowImmediatelyDataLoadEventListener();
                var request = SetupRequestObject(projDir, rootDir, identifierMap, listener);

                var extractionComponent = new DRSImageExtraction
                {
                    DatasetName = new Regex(".*"),
                    FilenameColumnName = "Image_Filename",
                    ImageUriColumnName = "ImageArchiveUri",
                    PathToImageArchive = rootDir.FullName
                };

                extractionComponent.PreInitialize(request, listener);

                var cts = new GracefulCancellationTokenSource();
                Assert.DoesNotThrow(() => extractionComponent.ProcessPipelineData(dataset, listener, cts.Token));

            }
            catch (Exception)
            {
                rootDir.Delete(true);
                throw;
            }
        }

        private IExtractDatasetCommand SetupRequestObject(string projDir, DirectoryInfo rootDir, DataTable identifierMap, IDataLoadEventListener listener)
        {
            var catalogue = MockRepository.GenerateStub<ICatalogue>();
            var loadMetadata = new LoadMetadata(CatalogueRepository);
            loadMetadata.LocationOfFlatFiles = projDir;
            loadMetadata.SaveToDatabase();
            catalogue.Stub(c => c.LoadMetadata).Return(loadMetadata);

            var datasetBundle = MockRepository.GenerateStub<IExtractableDatasetBundle>();
            var extractableDataset = MockRepository.GenerateStub<IExtractableDataSet>();
            datasetBundle.Stub(b => b.DataSet).Return(extractableDataset);

            var extractionDirectory = MockRepository.GenerateStub<IExtractionDirectory>();
            extractionDirectory.Stub(d => d.GetDirectoryForDataset(Arg<IExtractableDataSet>.Is.Anything)).Return(rootDir);

            var cohort = MockRepository.GenerateStub<IExtractableCohort>();
            cohort.Stub(c => c.GetPrivateIdentifier()).Return("PrivateID");
            cohort.Stub(c => c.GetReleaseIdentifier()).Return("ReleaseID");

            var request = MockRepository.GenerateStub<IExtractDatasetCommand>();
            request.Stub(r => r.Catalogue).Return(catalogue);
            request.Stub(r => r.DatasetBundle).Return(datasetBundle);

            var extractableColumn = MockRepository.GenerateStub<IColumn>();
            extractableColumn.Stub(c => c.GetRuntimeName()).Return("CHI");
            extractableColumn.Stub(c => c.IsExtractionIdentifier).Return(true);

            var queryTimeColumn = new QueryTimeColumn(extractableColumn);

            var queryBuilder = MockRepository.GenerateStub<ISqlQueryBuilder>();
            queryBuilder.Stub(qb => qb.SelectColumns).Return(new[] {queryTimeColumn}.ToList());

            request.ColumnsToExtract = new List<IColumn> {extractableColumn};
            request.Directory = extractionDirectory;
            request.ExtractableCohort = cohort;
            request.QueryBuilder = queryBuilder;
            return request;
        }
    }
}