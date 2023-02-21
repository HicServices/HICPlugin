using CsvHelper;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

public class DrsMultiVolumeRarAttacher : Attacher, IPluginAttacher
{
    [DemandsInitialization("The name of the column in the manifest file which contains the names of the image files")]
    public string FilenameColumnName { get; set; }

    [DemandsInitialization("The name of the column in RAW which stores the URI for each image in the DRS Image Archive")]
    public string ImageArchiveUriColumnName { get; set; }

    [DemandsInitialization("The filename of the manifest file (e.g. GoDARTSv2.csv)")]
    public string ManifestFileName { get; set; }

    [DemandsInitialization("Root archive path, with files moved from ForLoading without zipping (overrides the framework behaviour of zipping the contents of ForLoading and sticking them in ForArchiving).")]
    public DirectoryInfo ArchivePath { get; set; }

    [DemandsInitialization("Max uncompressed size of archives created during load (in bytes)")]
    public int MaxUncompressedSize { get; set; }

    [DemandsInitialization("Skip archive integrity check (intended for test loads only)")]
    public bool SkipArchiveIntegrityCheck { get; set; }

    [DemandsInitialization("Name of data table which holds the phenotypic data in the Diabetic Retinopathy database (i.e. the table to be loaded)")]
    public string TableName { get; set; }

    private readonly string[] _permittedImageExtensions = {"*.jpg", ".jpeg", ".png"};
    private string _existingExtractionDirectory;

    public DrsMultiVolumeRarAttacher() : base(true)
    {
        TableName = null;
    }


    // hack: (until we implement pipelines for DLE) I don't want the framework archiving the image data as it will always zip it to a fixed location. So we copy it to a special directory that is ignored by the framework archiver. It then still exists when the dispose part of the attacher's lifecycle is triggered (which happens after the fraemwork archiver has done its thing).
    private DirectoryInfo GetSuperSecretDirectory()
    {
        var superSecretDirectory = Path.Combine(LoadDirectory.ForLoading.FullName, "__hidden_from_archiver__");
        if (!Directory.Exists(superSecretDirectory))
            Directory.CreateDirectory(superSecretDirectory);

        return new DirectoryInfo(superSecretDirectory);
    }

    private ExtractedMultiVolumeRarProvider CreateExtractedArchiveProvider(DirectoryInfo workingDir, IDataLoadEventListener listener)
    {
        var archiveProvider = new ExtractedMultiVolumeRarProvider(workingDir.FullName, listener);
            
        // The images may already have been extracted from the archive, so check for them
        _existingExtractionDirectory = Directory.EnumerateDirectories(workingDir.FullName, "Images").SingleOrDefault();
        if (_existingExtractionDirectory != null)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Using existing image files in {_existingExtractionDirectory}"));
            archiveProvider.UseExistingExtractionDirectory(_existingExtractionDirectory);
        }

        return archiveProvider;
    }

    public override ExitCodeType Attach(IDataLoadJob job,GracefulCancellationToken token)
    {
        var workingDir = LoadDirectory.ForLoading;

        // This one was annihilating performance with very large multi-volume archives
        // using (var archiveProvider = new MultiVolumeRarProvider(workingDir.FullName, job))

        using (var archiveProvider = CreateExtractedArchiveProvider(workingDir, job))
        {
            Process(archiveProvider, job);
        }

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleting archive files from ForLoading"));

        foreach (var aFile in LoadDirectory.ForLoading.EnumerateFiles("*.rar", SearchOption.TopDirectoryOnly))
        {
            aFile.Delete();
        }

        // if we had been provided with an existing extraction directory, delete it now so it doesn't end up in ForArchiving
        if (!string.IsNullOrWhiteSpace(_existingExtractionDirectory))
        {
            // Don't use the nuclear option Directory.Delete(true), just in case we've somehow ended up pointing somewhere we shouldn't be
            foreach (var aFile in Directory.EnumerateFiles(_existingExtractionDirectory))
            {
                File.Delete(aFile);
            }
            Directory.Delete(_existingExtractionDirectory);
        }

        return ExitCodeType.Success;
    }

    private void Process(IArchiveProvider archiveProvider, IDataLoadJob job)
    {
        // Create directory to hold stripped image files
        var tempDirPath = Path.Combine(LoadDirectory.ForLoading.FullName, Path.GetRandomFileName());
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Creating temporary directory at {tempDirPath}"));
        var tempDir = Directory.CreateDirectory(tempDirPath);

        // Read each file from the archive and patch out the exif
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Reading number of entries in archive file..."));
        var numEntries = archiveProvider.GetNumEntries();
        job.OnNotify(this, new NotifyEventArgs(numEntries == 0 ? ProgressEventType.Warning : ProgressEventType.Information,
            $"Found {numEntries} entries when looking for {string.Join(", ", _permittedImageExtensions)}"));

        var patcherFactory = new CachedPatcherFactory();

        var entryNum = 0;
        var sw = new Stopwatch();
        sw.Start();
        foreach (var entry in archiveProvider.EntryStreams)
        {
            var name = entry.Key;
            var stream = entry.Value;

            var extension = Path.GetExtension(name);
            if (extension == null)
                throw new InvalidOperationException($"Could not recover the file extension of this entry: {name}");

            job.OnProgress(this, new ProgressEventArgs("Retrieving and Patching image files", new ProgressMeasurement(entryNum, ProgressType.Records), sw.Elapsed));

            var patcher = patcherFactory.Create(extension);
            using (var destStream = File.OpenWrite(Path.Combine(tempDir.FullName, name)))
            {
                patcher.PatchAwayExif(stream, destStream);
            }

            stream.Dispose();
            ++entryNum;
        }

        if (!SkipArchiveIntegrityCheck)
        {
            var checker = new ImageIntegrityChecker();
            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Verifying integrity of stripped images"));
            checker.VerifyIntegrityOfStrippedImages(archiveProvider, tempDir.FullName, job);
        }
        // Now zip up the directory, this will be archived once the load process is finished and the disposal method is called
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Archiving images (created archives will be moved to {ArchivePath.FullName} once the load is successfully completed."));
        ArchiveImagesForStorage(job, tempDir);

        // We're done with the rar files and temp directory
        tempDir.Delete(true);
    }

    private void ArchiveImagesForStorage(IDataLoadJob job, DirectoryInfo tempDir)
    {
        var processor = new ImageArchiveProcessor(tempDir, GetSuperSecretDirectory(), job.JobID);
        var archiveMappings = processor.ArchiveImagesForStorage(job, MaxUncompressedSize);

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Updating [{ImageArchiveUriColumnName}] in RAW with the correct location of each image in the DRS Image Archive"));

        foreach (var kvp in archiveMappings)
        {
            if (!kvp.Value.Any())
                throw new InvalidOperationException($"There are no file mappings for archive: {kvp.Key}");

            var query =
                $"UPDATE [{TableName}] SET {ImageArchiveUriColumnName} = CONCAT('{kvp.Key}!', {FilenameColumnName}) WHERE {FilenameColumnName} IN ('{string.Join("','", kvp.Value)}')";

            var server = _dbInfo.Server;
            using (var conn = server.GetConnection())
            {
                conn.Open();
                using (var cmd = server.GetCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public override void Check(ICheckNotifier notifier)
    {
        // Expect only rar files in ForLoading
        var filesInForLoading = LoadDirectory.ForLoading.EnumerateFiles().ToList();
        if (!filesInForLoading.Any())
        {
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"No files found in ForLoading: {LoadDirectory.ForLoading.FullName}", CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs("Files found in ForLoading", CheckResult.Success));

        // Check that we have the manifest file
        var manifestFilepath = Path.Combine(LoadDirectory.ForLoading.FullName, ManifestFileName);
        if (!File.Exists(manifestFilepath))
        {
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"No manifest file found in ForLoading: {LoadDirectory.ForLoading.FullName}", CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs($"Manifest file found: {ManifestFileName}", CheckResult.Success));

        var rarFileCount = filesInForLoading.Count(f => f.Extension == ".rar");
        if (rarFileCount == 0)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("ForLoading doesn't contain any rar files.", CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs($"ForLoading contains {rarFileCount} archive files", CheckResult.Success));

        // Now check that each of the zip files contains the correct files
        using (var archiveProvider = CreateExtractedArchiveProvider(LoadDirectory.ForLoading, new ThrowImmediatelyDataLoadEventListener()))
        {
            CheckImageArchive(archiveProvider, notifier);
        }
    }

    private void CheckImageArchive(IArchiveProvider archiveProvider, ICheckNotifier notifier)
    {
        notifier.OnCheckPerformed(new CheckEventArgs("Calculating number of entries...", CheckResult.Success));
        var numEntries = archiveProvider.GetNumEntries();
        if (numEntries == 0)
        {
            notifier.OnCheckPerformed(new CheckEventArgs($"Archive is empty! - {archiveProvider.Name}", CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs($"{numEntries} files found across all archives", CheckResult.Success));

        var archiveExtensions = archiveProvider.EntryNames.Select(Path.GetExtension).Distinct().ToList();
        var permittedFileExtensionsInArchive = _permittedImageExtensions.Intersect(archiveExtensions).ToList();
        if (!permittedFileExtensionsInArchive.Any())
            notifier.OnCheckPerformed(new CheckEventArgs("The archive contains no permitted files", CheckResult.Fail));

        var unexpectedFileExtensions = archiveExtensions.Except(_permittedImageExtensions).ToList();
        if (unexpectedFileExtensions.Any())
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"Unexpected file extensions in {archiveProvider.Name}: {string.Join(",", unexpectedFileExtensions)}", CheckResult.Fail));

        CheckManifestAgreement(archiveProvider, notifier);
    }

    private void CheckManifestAgreement(IArchiveProvider archiveProvider, ICheckNotifier notifier)
    {
        notifier.OnCheckPerformed(new CheckEventArgs("Checking that the manifest agrees with the images in the archive", CheckResult.Success));
            
        var imageFilenamesInArchive = archiveProvider.EntryNames.Select(Path.GetFileName).ToList();
        var imageFilenamesInManifest = new List<string>();

        var manifestFilepath = Path.Combine(LoadDirectory.ForLoading.FullName, ManifestFileName);
        using (var stream = File.OpenRead(manifestFilepath))
        {
            using (var sr = new StreamReader(stream))
            {
                using (var csvReader = new CsvReader(sr, Culture))
                {
                    while (csvReader.Read())
                    {
                        if(csvReader.Context.Reader.HeaderRecord == null)
                        {
                            csvReader.ReadHeader();
                            continue;
                        }

                        imageFilenamesInManifest.Add(csvReader[FilenameColumnName]);
                    }
                }
            }
        }

        var notInArchive = imageFilenamesInManifest.Except(imageFilenamesInArchive).ToList();
        if (notInArchive.Any())
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"These files are specified in the manifest but are not present in the archive: {string.Join(",", notInArchive)}", CheckResult.Fail));

        var notInManifest = imageFilenamesInArchive.Except(imageFilenamesInManifest).ToList();
        if (notInManifest.Any())
            notifier.OnCheckPerformed(new CheckEventArgs(
                $"These files are present in the archive but are not specified in the manifest: {string.Join(",", notInManifest)}", CheckResult.Fail));
    }

    public override void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener listener)
    {
        var directoryForArchive = GetSuperSecretDirectory();

        // This archive will have directories in it
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"The remainder of the load has completed successfully, now moving all archived images to the DRS Image Archive at {ArchivePath.FullName}"));
        foreach (var dir in directoryForArchive.EnumerateDirectories())
        {
            Directory.Move(dir.FullName, Path.Combine(ArchivePath.FullName, dir.Name));
        }

        directoryForArchive.Delete(true);
    }
}