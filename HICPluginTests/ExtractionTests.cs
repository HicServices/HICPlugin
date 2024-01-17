using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using DrsPlugin.Extraction;
using HICPluginTests;
using ICSharpCode.SharpZipLib.Tar;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataExport.DataExtraction.Commands;
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

        var extractionIdentifierColumn = new MockColumn("ReleaseID");

        var replacer = new DRSFilenameReplacer(extractionIdentifierColumn, "Image_Filename");

        string[] renameParams = {"Examination_Date", "Image_Num"};
       // Assert.Equals("R00001_2016-05-17_1.png", replacer.GetCorrectFilename(dataset.Rows[0],renameParams,null));
    }

    [Test]
    public void ExtractionTestWithZipArchive()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "1"));
        File.WriteAllText(Path.Combine(tempDir.FullName, "2_P12345_2016-05-07_1.png"), "");

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
        dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_1.png", @"1\1.zip!2_P12345_2016-05-07_1.png");

        try
        {
            var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
            var request = SetupRequestObject(projDir, rootDir);

            var extractionComponent = new DRSImageExtraction
            {
                DatasetName = new Regex(".*"),
                FilenameColumnName = "Image_Filename",
                ImageUriColumnName = "ImageArchiveUri",
                FileNameReplacementColumns = "Examination_Date,Image_Num",
                PathToImageArchive = archiveDir.FullName,
                AppendIndexCountToFileName = false
            };

            extractionComponent.PreInitialize(request, listener);

            var cts = new GracefulCancellationTokenSource();
            var dt = extractionComponent.ProcessPipelineData(dataset, listener, cts.Token);

            var imageDir = rootDir.EnumerateDirectories("Images").SingleOrDefault();
            Assert.That(imageDir, Is.Not.Null);// "Extraction directory to hold images has not been created");

            var imageFiles = imageDir.EnumerateFiles().ToList();
           // Assert.Equals(1, imageFiles.Count);
           // Assert.Equals("R00001_2016-05-17_1.png", imageFiles[0].Name);

            Assert.That(dt.Columns.Contains("ImageArchiveUri"), Is.False);
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
        var imagePath = Path.Combine(tempDir.FullName, "2_P12345_2016-05-07_1.png");
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
        dataset.Rows.Add("R00001", @"17/05/2016", "R", "1", "1024", "768", "2_P12345_2016-05-07_1.png", @"1\1.tar!2_P12345_2016-05-07_1.png");

        try
        {
            var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
            var request = SetupRequestObject(projDir, rootDir);

            var extractionComponent = new DRSImageExtraction
            {
                DatasetName = new Regex(".*"),
                FilenameColumnName = "Image_Filename",
                ImageUriColumnName = "ImageArchiveUri",
                FileNameReplacementColumns = "Examination_Date,Image_Num",
                PathToImageArchive = archiveDir.FullName,
                AppendIndexCountToFileName = false
            };

            extractionComponent.PreInitialize(request, listener);

            var cts = new GracefulCancellationTokenSource();
            var dt = extractionComponent.ProcessPipelineData(dataset, listener, cts.Token);

            var imageDir = rootDir.EnumerateDirectories("Images").SingleOrDefault();
            Assert.That(imageDir, Is.Not.Null);// "Extraction directory to hold images has not been created");

            var imageFiles = imageDir.EnumerateFiles().ToList();
           // Assert.Equals(1, imageFiles.Count);
           // Assert.Equals("R00001_2016-05-17_1.png", imageFiles[0].Name);

            Assert.That(dt.Columns.Contains("ImageArchiveUri"), Is.False);
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
            var request = SetupRequestObject(projDir, rootDir);

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

    private IExtractDatasetCommand SetupRequestObject(string projDir, DirectoryInfo rootDir)
    {
        var loadMetadata = new LoadMetadata(CatalogueRepository)
        {
            LocationOfFlatFiles = projDir
        };
        loadMetadata.SaveToDatabase();

        var catalogue = new MockCatalogue(loadMetadata);

        var extractionDirectory = new MockExtractionDirectory(rootDir);

        var extractableColumn = new MockColumn("CHI", true);
        var queryTimeColumn = new QueryTimeColumn(extractableColumn);
        var queryBuilder = new MockSqlQueryBuilder(new List<QueryTimeColumn> { queryTimeColumn });
        return new MockExtractDatasetCommand(catalogue, extractionDirectory, new List<IColumn> { extractableColumn }, queryBuilder);
    }
}