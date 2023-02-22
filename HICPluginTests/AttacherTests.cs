using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DrsPlugin.Attachers;
using FAnsi;
using FAnsi.Discovery;
using ICSharpCode.SharpZipLib.Tar;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Modules.Attachers;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace DrsPluginTests;

public class AttacherTests : DatabaseTests
{
    private string _databaseName;
    private readonly IDataLoadJob job = Mock.Of<IDataLoadJob>(j => j.JobID == 1);


    [OneTimeSetUp]
    public void BeforeAnyTests()
    {
        _databaseName = $"DRS_TEST_{Guid.NewGuid()}";
        var server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn;
        if (server.ExpectDatabase(_databaseName).Exists())
            Assert.Fail(
                $"Could not continue with tests, there is a test database on {server.Name} called {_databaseName}. Please delete this manually before re-running the tests.");

        server.CreateDatabase(_databaseName);

        var tableCreationQuery = $@"CREATE TABLE [{_databaseName}].[dbo].[GoDARTSv2_TEST](
	[PX_Count] [int] NULL,
	[CHI] [decimal](10, 0) NULL,
	[Examination_Date] [datetime] NULL,
	[Visual_Acuity] [varchar](11) NULL,
	[Eye] [varchar](1) NULL,
	[Retinopathy] [varchar](2) NULL,
	[Maculopathy] [varchar](2) NULL,
	[Image_Num] [int] NULL,
	[Image_Filename] [varchar](46) NULL,
	[Pixel_Width] [int] NULL,
	[Pixel_Height] [int] NULL,
	[blotHaem] [varchar](4) NULL,
	[CWS] [varchar](2) NULL,
	[flameHaem] [varchar](2) NULL,
	[imageQuality] [varchar](29) NULL,
	[IRMA] [varchar](2) NULL,
	[laserScarsType] [varchar](2) NULL,
	[MA] [varchar](2) NULL,
	[MacBlotHaem] [varchar](2) NULL,
	[MaxExudates] [varchar](2) NULL,
	[NVD] [varchar](4) NULL,
	[NVE] [varchar](4) NULL,
	[OODARMD] [varchar](2) NULL,
	[OODAstHyal] [varchar](2) NULL,
	[OODCuppedDisc] [varchar](3) NULL,
	[OODDrusen] [varchar](2) NULL,
	[OODEpiretMemb] [varchar](2) NULL,
	[OODMRNF] [varchar](2) NULL,
	[OODPigmLesion] [varchar](2) NULL,
	[OODRetVeinThromb] [varchar](2) NULL,
	[RetDet] [varchar](2) NULL,
	[VB] [varchar](2) NULL,
	[VH] [varchar](2) NULL,
    [ImageArchiveRelativeUri] [varchar](1024) NULL
) ON [PRIMARY]";

        server.ChangeDatabase(_databaseName);
        using var con = server.GetConnection();
        con.Open();
        var cmd = DatabaseCommandHelper.GetCommand(tableCreationQuery, con);
        cmd.ExecuteNonQuery();
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        // Delete the dataset database
        var database = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName);
        if (!database.Exists())
            throw new Exception($"Something has unexpectedly deleted the test dataset database: {_databaseName}");

        // Try to make sure we have the correct database (shouldn't need to do this, it's named with a GUID after all, but let's be safe)
        var tables = database.DiscoverTables(true).ToList();
        if (tables.Count > 1 || tables.Count == 0)
            throw new Exception($"{_databaseName} should have 1 table in it (GoDARTSv2_TEST) but has {tables.Count}");

        if (tables[0].GetRuntimeName() != "GoDARTSv2_TEST")
            throw new Exception(
                $"{_databaseName} has 1 table but it is not called GoDARTSv2_TEST (it is called {tables[0].GetRuntimeName()}");

        tables[0].Drop();
        database.Drop();
    }

    [Test]
    public void Checking_NoFilesInForLoading()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        try
        {

            var LoadDirectory = new Mock<ILoadDirectory>();
            LoadDirectory.Setup(d => d.ForLoading).Returns(testDir);
            var attacher = new DrsMultiVolumeRarAttacher();
            attacher.Initialize(LoadDirectory.Object, db);

            var ex = Assert.Throws<Exception>(() => attacher.Check(new ThrowImmediatelyCheckNotifier()));

            Assert.AreEqual($"No files found in ForLoading: {testDir.FullName}", ex?.Message);
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void Checking_CorrectlyFormedArchive()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName()));
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        try
        {
            ProvisionTestData(testDir);

            var LoadDirectory = new Mock<ILoadDirectory>();
            LoadDirectory.Setup(d => d.ForLoading).Returns(testDir);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename"
            };
            attacher.Initialize(LoadDirectory.Object, db);

            Assert.DoesNotThrow(() => attacher.Check(new ThrowImmediatelyCheckNotifier()));
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void Checking_FailOnNonEmptyScratchDirectory()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var scratchDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        File.WriteAllText(Path.Combine(scratchDir.FullName, "rogue_file.txt"), "foo");
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        try
        {
            ProvisionTestData(testDir);

            var LoadDirectory = new Mock<ILoadDirectory>();
            LoadDirectory.Setup(d => d.ForLoading).Returns(testDir);

            var attacher = new DrsFileAttacher()
            {
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename",
                SecureLocalScratchArea = scratchDir
            };
            attacher.Initialize(LoadDirectory.Object, db);

            var ex = Assert.Throws<Exception>(() => attacher.Check(new ThrowImmediatelyCheckNotifier()));
            Assert.AreEqual(ex.Message, "SecureLocalScratchArea is not empty, please ensure it is empty before attempting to attach.");
        }
        finally
        {
            testDir.Delete(true);
            scratchDir.Delete(true);
        }
    }

    [Test]
    public void Checking_ManifestMismatch_MissingImages()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName()));
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        try
        {
            ProvisionTestData(testDir, TestData.TestData.DRS_RETINAL_TEST_MANIFEST_ADDITIONAL_ENTRY);

            var LoadDirectory = new Mock<ILoadDirectory>();
            LoadDirectory.Setup(d => d.ForLoading).Returns(testDir);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename"
            };
            attacher.Initialize(LoadDirectory.Object, db);

            var ex = Assert.Throws<Exception>(() => attacher.Check(new ThrowImmediatelyCheckNotifier()));
            Assert.AreEqual("These files are specified in the manifest but are not present in the archive: 2_2345678901_2016-05-19_RM_1_PW1024_PH768.png", ex?.Message);
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void Checking_ManifestMismatch_MissingManifestEntries()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName()));
        var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
        try
        {
            ProvisionTestData(testDir, TestData.TestData.DRS_RETINAL_TEST_MANIFEST_MISSING_ENTRY);

            var LoadDirectory = new Mock<ILoadDirectory>();
            LoadDirectory.Setup(d => d.ForLoading).Returns(testDir);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename"
            };
            attacher.Initialize(LoadDirectory.Object, db);

            var ex = Assert.Throws<Exception>(() => attacher.Check(new ThrowImmediatelyCheckNotifier()));
            Assert.AreEqual("These files are present in the archive but are not specified in the manifest: 2_2345678901_2016-05-18_LM_2_PW1024_PH768.png", ex?.Message);
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void AttacherTest_Success()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var archiveDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var loadDirectory = LoadDirectory.CreateDirectoryStructure(testDir, "DRS");

        try
        {
            // put the test data in ForLoading
            ProvisionTestData(loadDirectory.ForLoading);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                TableName = "GoDARTSv2_TEST",
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename",
                ImageArchiveUriColumnName = "ImageArchiveRelativeUri",
                ArchivePath = archiveDir,
                MaxUncompressedSize = 1024 * 1024 // should result in 3 separate archives
            };

            attacher.Initialize(loadDirectory, DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName));
            attacher.Attach(job,new GracefulCancellationToken());
            attacher.LoadCompletedSoDispose(ExitCodeType.Success, new ThrowImmediatelyDataLoadEventListener());

            // Should now only be the CSV file in ForLoading (which would be archived during the real load process)
            Assert.IsTrue(File.Exists(Path.Combine(loadDirectory.ForLoading.FullName, attacher.ManifestFileName)));

            // Should be three zip files in the archive
            Assert.AreEqual(new[] { "1_1.tar", "1_2.tar", "1_3.tar" },
                archiveDir.EnumerateFiles("*.tar", SearchOption.AllDirectories)
                    .Select(f => f.Name).ToArray());
        }
        finally
        {
            try
            {
                testDir.Delete(true);
                archiveDir.Delete(true);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private DiscoveredTable GetPhenotypeTable()
    {
        var database = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName);
        return database.DiscoverTables(false).Single(t => t.GetRuntimeName() == "GoDARTSv2_TEST");
    }

    private void TruncatePhenotypeTable()
    {
        // Remove data from the test dataset database
        var table = GetPhenotypeTable();
        var query = $"TRUNCATE TABLE [{_databaseName}]..[{table.GetRuntimeName()}]";
        using var con = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.GetConnection();
        con.Open();
        var cmd = DatabaseCommandHelper.GetCommand(query, con);
        cmd.ExecuteNonQuery();
    }

    [Test]
    public void AttacherTest_SuccessWithPreExtractedArchive()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var archiveDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var loadDirectory = LoadDirectory.CreateDirectoryStructure(testDir, "DRS");

        try
        {
            // put the test data in ForLoading
            ProvisionTestData(loadDirectory.ForLoading);

            // Pre-extract the archive
            var rarHelper = new RarHelper();
            var extractionDir = loadDirectory.ForLoading.CreateSubdirectory("Images");
            rarHelper.ExtractMultiVolumeArchive(loadDirectory.ForLoading.FullName, extractionDir.FullName);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                TableName = "GoDARTSv2_TEST",
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename",
                ArchivePath = archiveDir,
                ImageArchiveUriColumnName = "ImageArchiveRelativeUri",
                MaxUncompressedSize = 1024 * 1024 // should result in 3 separate archives
            };
            attacher.Initialize(loadDirectory, DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName));

            attacher.Attach(job,new GracefulCancellationToken());
            attacher.LoadCompletedSoDispose(ExitCodeType.Success, new ThrowImmediatelyDataLoadEventListener());

            // Should now only be the CSV file in ForLoading (which would be archived during the real load process)
            Assert.IsTrue(File.Exists(Path.Combine(loadDirectory.ForLoading.FullName, attacher.ManifestFileName)));

            // Should be three zip files in the archive
            Assert.AreEqual(new[] { "1_1.tar", "1_2.tar", "1_3.tar" },
                archiveDir.EnumerateFiles("*.tar", SearchOption.AllDirectories)
                    .Select(f => f.Name).ToArray());
        }
        finally
        {
            try
            {
                testDir.Delete(true);
                archiveDir.Delete(true);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    [Test]
    public void EnsureImagePixelDataIsNotCorrupted()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var archiveDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var loadDirectory = LoadDirectory.CreateDirectoryStructure(testDir, "DRS");

        try
        {
            // put the test data in ForLoading
            ProvisionTestData(loadDirectory.ForLoading);

            var attacher = new DrsMultiVolumeRarAttacher()
            {
                TableName = "GoDARTSv2_TEST",
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename",
                ArchivePath = archiveDir,
                ImageArchiveUriColumnName = "ImageArchiveRelativeUri",
                MaxUncompressedSize = 1024*1024 // should result in 3 separate archives
            };

            attacher.Initialize(loadDirectory, DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName));
            attacher.Attach(job, new GracefulCancellationToken());
            attacher.LoadCompletedSoDispose(ExitCodeType.Success, new ThrowImmediatelyDataLoadEventListener());

            // The stripped files will now be in the archive so unzip them
            foreach (var archive in archiveDir.EnumerateFiles("*.tar", SearchOption.AllDirectories).ToList())
            {
                using var tar = TarArchive.CreateInputTarArchive(File.OpenRead(archive.FullName));
                tar.ExtractContents(archiveDir.FullName);
            }

            // We need to re-provision the rar archives so we can check them
            ProvisionTestData(loadDirectory.ForLoading);

            var integrityChecker = new ImageIntegrityChecker();
            using var archiveProvider = new ExtractedMultiVolumeRarProvider(loadDirectory.ForLoading.FullName, new ThrowImmediatelyDataLoadEventListener());
            Assert.DoesNotThrow(() => integrityChecker.VerifyIntegrityOfStrippedImages(archiveProvider, archiveDir.FullName, new ThrowImmediatelyDataLoadEventListener()));
        }
        finally
        {
            testDir.Delete(true);
            archiveDir.Delete(true);
        }
    }

    private void ProvisionTestData(DirectoryInfo dataDirectory, string manifestFileData = null)
    {
        const string mainFilename = "DRS_Retinal_Test";
        manifestFileData = manifestFileData ?? TestData.TestData.DRS_RETINAL_TEST_MANIFEST;

        // Copy the split RAR archive into testDir
        var resourcesToCopy = new[]
        {
            TestData.TestData.DRS_RETINAL_TEST_PART1,
            TestData.TestData.DRS_RETINAL_TEST_PART2,
            TestData.TestData.DRS_RETINAL_TEST_PART3,
            TestData.TestData.DRS_RETINAL_TEST_PART4,
            TestData.TestData.DRS_RETINAL_TEST_PART5
        };

        for (var i = 0; i < resourcesToCopy.Length; ++i)
        {
            File.WriteAllBytes(Path.Combine(dataDirectory.FullName, $"{mainFilename}.part{(i + 1)}.rar"), resourcesToCopy[i]);
        }

        File.WriteAllText(Path.Combine(dataDirectory.FullName, "GoDARTSv2.csv"), manifestFileData);
    }

    [Test]
    public void IntegrityChecker_DetectsDamagedImage()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var loadDirectory = LoadDirectory.CreateDirectoryStructure(testDir, "DRS");

        try
        {
            // put the test data in ForLoading
            ProvisionTestData(loadDirectory.ForLoading);

            // Create a corrupt image file, named the same as one of the files in the archive
            var imageDir = loadDirectory.ForLoading.CreateSubdirectory(Path.GetRandomFileName());
            var imagePath = Path.Combine(imageDir.FullName, "2_1234567890_2016-05-17_LM_2_PW1024_PH768.jpeg");
            File.WriteAllBytes(imagePath, new byte[]
            {
                0xff, 0xd8, 0xff, 0xff, 0xff, 0xff
            });

            var integrityChecker = new ImageIntegrityChecker();
            using var archiveProvider = new ExtractedMultiVolumeRarProvider(loadDirectory.ForLoading.FullName, new ThrowImmediatelyDataLoadEventListener());
            var ex = Assert.Throws<InvalidOperationException>(() => integrityChecker.VerifyIntegrityOfStrippedImages(archiveProvider, imageDir.FullName, new ThrowImmediatelyDataLoadEventListener()), 
                "The image pixel data does not match so this method should throw. Otherwise it is reporting that there is no integrity problem when there is one.");

            Assert.IsTrue(ex?.Message.Contains("The pixel byte array lengths are different"));
            Console.WriteLine(ex?.Message);
        }
        finally
        {
            testDir.Delete(true);
        }
    }


    [Test]
    public void OpenMultiFileArchive()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            ProvisionTestData(testDir);

            var rarHelper = new RarHelper();
            rarHelper.ExtractMultiVolumeArchive(testDir);

            // Now check we have the correct files
            Assert.AreEqual(2, testDir.EnumerateFiles("*.jpeg").Count());
            Assert.AreEqual(2, testDir.EnumerateFiles("*.png").Count());

        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void TestImageArchiveProcessing()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            ProvisionTestData(testDir, TestData.TestData.DRS_RETINAL_TEST_MANIFEST_ADDITIONAL_ENTRY);
            var helper = new RarHelper();
            helper.ExtractMultiVolumeArchive(testDir);

            var files = testDir.EnumerateFiles().ToList();

            // Remove any test data that isn't an image file
            foreach (var imgFile in files.Except(files.Where(f => f.Extension == ".jpeg" || f.Extension == ".png")))
            {
                imgFile.Delete();   
            }

            Assert.AreEqual(4, testDir.EnumerateFiles().Count());

            var processor = new ImageArchiveProcessor(testDir, testDir, 1);
            processor.ArchiveImagesForStorage(new ThrowImmediatelyDataLoadEventListener(), 1024 * 1024);

            var imageDirPath = Path.Combine(testDir.FullName, "1");
            Assert.IsTrue(Directory.Exists(imageDirPath));

            var remainingFiles = Directory.EnumerateFiles(imageDirPath).ToList();
            Assert.AreEqual(3, remainingFiles.Count);
            Assert.AreEqual(3, remainingFiles.Count(f => Path.GetExtension(f) == ".tar"));
            Assert.AreEqual(new[] { "1_1.tar", "1_2.tar", "1_3.tar" }, remainingFiles.Select(Path.GetFileName).ToArray());
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public void AttacherWithFilesystemArchiveProvider()
    {
        var testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var scratchDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var archiveDir = testDir.CreateSubdirectory("archive");
        var loadDirectory = LoadDirectory.CreateDirectoryStructure(testDir, "DRS");

        try
        {
            ProvisionTestData(loadDirectory.ForLoading, TestData.TestData.DRS_RETINAL_TEST_MANIFEST);
            var helper = new RarHelper();
            helper.ExtractMultiVolumeArchive(loadDirectory.ForLoading);

            // Create directory structure analagous to that received in the full extract
            var dir1 = loadDirectory.ForLoading.CreateSubdirectory("GoDARTS01-02");
            var dir2 = loadDirectory.ForLoading.CreateSubdirectory("GoDARTS03-04");
            foreach (var jpeg in loadDirectory.ForLoading.EnumerateFiles("*.jpeg"))
            {
                File.Move(jpeg.FullName, Path.Combine(dir1.FullName, jpeg.Name));
            }
            foreach (var png in loadDirectory.ForLoading.EnumerateFiles("*.png"))
            {
                File.Move(png.FullName, Path.Combine(dir2.FullName, png.Name));
            }

            var database = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase(_databaseName);
            var csvAttacher = new AnySeparatorFileAttacher()
            {
                Separator = ",",
                FilePattern = "GoDARTSv2.csv",
                TableName = "GoDARTSv2_TEST",
                Culture = new CultureInfo("en-GB")
            };
            csvAttacher.Initialize(loadDirectory, database);

            csvAttacher.Attach(job, new GracefulCancellationToken());

            var attacher = new DrsFileAttacher()
            {
                TableName = "GoDARTSv2_TEST",
                ManifestFileName = "GoDARTSv2.csv",
                FilenameColumnName = "Image_Filename",
                ArchivePath = archiveDir,
                ImageArchiveUriColumnName = "ImageArchiveRelativeUri",
                SecureLocalScratchArea = scratchDir,
                MaxUncompressedSize = 1024 * 1024 // should result in 3 separate archives
            };
            attacher.Initialize(loadDirectory, database);

            attacher.Attach(job, new GracefulCancellationToken());
            attacher.LoadCompletedSoDispose(ExitCodeType.Success, new ThrowImmediatelyDataLoadEventListener());

            Assert.AreEqual(3, archiveDir.EnumerateFiles("*.tar", SearchOption.AllDirectories).Count(), 
                "There should be 3 zip files in the archive directory.");

            // Check that the ImageArchiveUriColumn has been updated in the database
            var uriListFromDatabase = new List<string>();
            var server = database.Server;
            server.ChangeDatabase(database.GetRuntimeName());
            using var con = server.GetConnection();
            con.Open();
            var query = "SELECT ImageArchiveRelativeUri FROM GoDARTSv2_TEST";
            var cmd = DatabaseCommandHelper.GetCommand(query, con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                uriListFromDatabase.Add(reader["ImageArchiveRelativeUri"].ToString());

            var expectedUriList = new List<string>
            {
                @"1\1_1.tar!2_1234567890_2016-05-17_RM_1_PW1024_PH768.jpeg",
                @"1\1_1.tar!2_1234567890_2016-05-17_LM_2_PW1024_PH768.jpeg",
                @"1\1_3.tar!2_2345678901_2016-05-18_RM_1_PW1024_PH768.png",
                @"1\1_2.tar!2_2345678901_2016-05-18_LM_2_PW1024_PH768.png"
            };

            Assert.IsTrue(!expectedUriList.Except(uriListFromDatabase).Any());
        }
        finally
        {
            testDir.Delete(true);
            scratchDir.Delete(true);
        }
    }
}