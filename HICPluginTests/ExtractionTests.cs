using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using DrsPlugin.Extraction;
using ICSharpCode.SharpZipLib.Tar;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataExport.DataExtraction.UserPicks;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common;

namespace DrsPluginTests;

public class ExtractionTests : DatabaseTests
{
    [Test]
    public void FilenameReplacerTest()
    {
        using var dataset = new DataTable("Dataset");
        dataset.Columns.Add("ReleaseID");
        dataset.Columns.Add("Examination_Date");
        dataset.Columns.Add("Eye");
        dataset.Columns.Add("Image_Num");
        dataset.Columns.Add("Pixel_Width");
        dataset.Columns.Add("Pixel_Height");
        dataset.Columns.Add("Image_Filename");
        dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_RM_1_PW1024_PH768.png");

        var extractionIdentifierColumn = new Mock<IColumn>();
        extractionIdentifierColumn.Setup(c => c.GetRuntimeName()).Returns("ReleaseID");

        var replacer = new DRSFilenameReplacer(extractionIdentifierColumn.Object, "Image_Filename");

        Assert.AreEqual("R00001_2016-05-17_RM_1_PW1024_PH768.png", replacer.GetCorrectFilename(dataset.Rows[0], ThrowImmediatelyDataLoadEventListener.Quiet));
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
            var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
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
            var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
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
            var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
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
        var loadMetadata = new LoadMetadata(CatalogueRepository)
        {
            LocationOfFlatFiles = projDir
        };
        loadMetadata.SaveToDatabase();

        var catalogue = Mock.Of<ICatalogue>(c=>c.LoadMetadata==loadMetadata);

        var extractableDataset = Mock.Of<IExtractableDataSet>();
        var datasetBundle = Mock.Of<IExtractableDatasetBundle>(
            dsb => dsb.DataSet == extractableDataset);

        var extractionDirectory = new Mock<IExtractionDirectory>();
        extractionDirectory.Setup(d => d.GetDirectoryForDataset(It.IsAny<IExtractableDataSet>())).Returns(rootDir);

        var cohort = new Mock<IExtractableCohort>();
        cohort.Setup(c => c.GetPrivateIdentifier(It.IsAny<bool>())).Returns("PrivateID");
        cohort.Setup(c => c.GetReleaseIdentifier(It.IsAny<bool>())).Returns("ReleaseID");

        var extractableColumn = new Mock<IColumn>();
        extractableColumn.Setup(c => c.GetRuntimeName()).Returns("CHI");
        extractableColumn.Setup(c => c.IsExtractionIdentifier).Returns(true);
        var queryTimeColumn = new QueryTimeColumn(extractableColumn.Object);
        var queryBuilder = new Mock<ISqlQueryBuilder>();
        queryBuilder.Setup(qb => qb.SelectColumns).Returns(new[] { queryTimeColumn }.ToList());
        var request = Mock.Of<IExtractDatasetCommand>(
            r =>
                r.Catalogue==catalogue &&
                r.DatasetBundle == datasetBundle &&
                r.ColumnsToExtract==new List<IColumn> {extractableColumn.Object} &&
                r.Directory==extractionDirectory.Object &&
                r.ExtractableCohort==cohort.Object &&
                r.QueryBuilder==queryBuilder.Object
        ); 
        return request;
    }
}