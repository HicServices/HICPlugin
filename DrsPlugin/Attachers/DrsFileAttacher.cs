using CsvHelper;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

public class DrsFileAttacher : Attacher, IPluginAttacher
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

    [DemandsInitialization("Path to secure directory where identifiable image data may be held temporarily, preferable local as the images may be accessed several times throughout the attaching process - this will help alleviate network load and increase processing speed. MUST POINT TO AN EMPTY DIRECTORY, THE ATTACHER WILL DELETE EVERYTHING FROM IT ON COMPLETION")]
    public DirectoryInfo SecureLocalScratchArea { get; set; }

    private readonly string[] _permittedImageExtensions = { ".jpg", ".jpeg", ".png" };

    public DrsFileAttacher() : base(true)
    {
        TableName = null;
    }

    public override ExitCodeType Attach(IDataLoadJob job,GracefulCancellationToken token)
    {
        if (SecureLocalScratchArea == null)
            throw new InvalidOperationException("SecureLocalScratchArea must be set before attempting to attach.");

        var workingDir = LoadDirectory.ForLoading;

        var archiveProvider = new FilesystemArchiveProvider(workingDir.FullName, _permittedImageExtensions, job);
        Process(archiveProvider, job);

        var doNotDelete = SecureLocalScratchArea.Name;
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleting image directories from ForLoading"));
        var directoriesToDelete = LoadDirectory.ForLoading.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
            .Where(d => d.Name != doNotDelete).ToList();
                
        foreach(var d in directoriesToDelete)
            d.Delete(true);

        return ExitCodeType.Success;
    }

    private void Process(IArchiveProvider archiveProvider, IDataLoadJob job)
    {
        // Create directory to hold stripped image files
        var tempDirPath = Path.Combine(SecureLocalScratchArea.FullName, Path.GetRandomFileName());
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Creating temporary directory at {tempDirPath}"));
        var tempDir = Directory.CreateDirectory(tempDirPath);

        var localCopyPath = Path.Combine(SecureLocalScratchArea.FullName, Path.GetRandomFileName());
        var localCopyDir = Directory.CreateDirectory(localCopyPath);

        // Read each file from the archive and patch out the exif
        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Reading number of entries in archive file..."));
        var numEntries = archiveProvider.GetNumEntries();
        job.OnNotify(this, new NotifyEventArgs(numEntries == 0 ? ProgressEventType.Warning : ProgressEventType.Information,
            $"Found {numEntries} entries when looking for {string.Join(", ", _permittedImageExtensions)}"));

        var patcherFactory = new CachedPatcherFactory();
        var processor = new ImageArchiveProcessor(tempDir, SecureLocalScratchArea, job.JobID);

        var entryNum = 0;
        var sw = new Stopwatch();
        var readTimer = new Stopwatch();
        var patchTimer = new Stopwatch();
        var chunkTimer = new Stopwatch();
        sw.Start();
        readTimer.Start();
        chunkTimer.Start();
        long chunkFileSize = 0;
        var chunkNum = 0;
        foreach (var (name, stream) in archiveProvider.EntryStreams)
        {
            job.OnProgress(this, new ProgressEventArgs("Total elapsed time", new ProgressMeasurement(entryNum, ProgressType.Records), sw.Elapsed));

            readTimer.Stop();
            job.OnProgress(this, new ProgressEventArgs("-- Retrieving files", new ProgressMeasurement(entryNum, ProgressType.Records), readTimer.Elapsed));

            var extension = Path.GetExtension(name) ?? throw new InvalidOperationException($"Could not recover the file extension of this entry: {name}");

            // Copy images to local scratch area so as to not require extra network traffic for future processing
            using (var localCopyStream = File.Open(Path.Combine(localCopyPath, name), FileMode.CreateNew))
            {
                stream.CopyTo(localCopyStream);

                // We're done with the copy from the ForLoading archive now
                stream.Dispose();

                // Rewind our local copy's stream
                localCopyStream.Position = 0;

                var patcher = patcherFactory.Create(extension);
                using var destStream = File.OpenWrite(Path.Combine(tempDir.FullName, name));
                chunkFileSize += localCopyStream.Length;
                patchTimer.Start();
                patcher.PatchAwayExif(localCopyStream, destStream);
                patchTimer.Stop();
                job.OnProgress(this,
                    new ProgressEventArgs("-- Patching/saving files",
                        new ProgressMeasurement(entryNum, ProgressType.Records), patchTimer.Elapsed));
            }
            ++entryNum;

            if (chunkFileSize > MaxUncompressedSize)
            {
                ++chunkNum;
                ArchiveChunk(processor, localCopyDir, tempDir, job, chunkNum, chunkTimer);
                chunkFileSize = 0;

                foreach (var aFile in localCopyDir.EnumerateFiles())
                {
                    aFile.Delete();
                }
            }

            readTimer.Start();
        }

        // The final chunk with the remainder of files in the scratch space
        ArchiveChunk(processor, localCopyDir, tempDir, job, chunkNum + 1, chunkTimer);

        // We're done with the rar files and temp directory
        tempDir.Delete(true);
        localCopyDir.Delete(true);
    }

    private void ArchiveChunk(ImageArchiveProcessor processor, DirectoryInfo localCopyDir, DirectoryInfo tempDir, IDataLoadJob job, int chunkNum, Stopwatch chunkTimer)
    {
        if (!SkipArchiveIntegrityCheck)
        {
            var checker = new ImageIntegrityChecker();
            job.OnProgress(this, new ProgressEventArgs("Verifying integrity of stripped images", new ProgressMeasurement(chunkNum, ProgressType.Records), chunkTimer.Elapsed));
            checker.VerifyIntegrityOfStrippedImages(localCopyDir.FullName, tempDir.FullName, job);
        }

        // archive the current batch in SecureLocalScratchArea
        job.OnProgress(this,
            new ProgressEventArgs("Creating image archives", new ProgressMeasurement(chunkNum, ProgressType.Records),
                chunkTimer.Elapsed));
        var archiveMappings = processor.ArchiveImagesForStorage(job);
        UpdateRAWWithImageArchiveLocations(job, archiveMappings);
    }

    private void UpdateRAWWithImageArchiveLocations(IDataLoadJob job, Dictionary<string, List<string>> archiveMappings)
    {
        job.OnNotify(this,
            new NotifyEventArgs(ProgressEventType.Information,
                $"Updating [{ImageArchiveUriColumnName}] in RAW with the correct location of each image in the DRS Image Archive"));

        using var conn = _dbInfo.Server.GetConnection();
        conn.Open();
        foreach (var (archive, entry) in archiveMappings)
        {
            if (!entry.Any())
                throw new InvalidOperationException($"There are no file mappings for archive: {archive}");

            var query =
                $"UPDATE [{TableName}] SET {ImageArchiveUriColumnName} = CONCAT('{archive}!', {FilenameColumnName}) WHERE {FilenameColumnName} IN ('{string.Join("','", entry)}')";

            using var cmd = _dbInfo.Server.GetCommand(query, conn);
            cmd.ExecuteNonQuery();
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

        // Now check that the directories contains the correct files
        /*
        using (var archiveProvider = new FilesystemArchiveProvider(LoadDirectory.ForLoading.FullName, _permittedImageExtensions))
        {
            CheckImageArchive(archiveProvider, notifier);
        }
         * */

        // Check we can access the directory at SecureLocalScratchArea
        if (!SecureLocalScratchArea.Exists)
        {
            notifier.OnCheckPerformed(
                new CheckEventArgs($"SecureLocalScratchArea does not exist: {SecureLocalScratchArea.FullName}",
                    CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs($"Found SecureLocalScratchArea: {SecureLocalScratchArea.FullName}", CheckResult.Success));

        if (SecureLocalScratchArea.EnumerateFileSystemInfos().Any())
        {
            notifier.OnCheckPerformed(
                new CheckEventArgs("SecureLocalScratchArea is not empty, please ensure it is empty before attempting to attach.", CheckResult.Fail));
            return;
        }
        notifier.OnCheckPerformed(new CheckEventArgs("SecureLocalScratchArea is empty.", CheckResult.Success));
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
            using var sr = new StreamReader(stream);
            using var csvReader = new CsvReader(sr, Culture);
            while (csvReader.Read())
                imageFilenamesInManifest.Add(csvReader[FilenameColumnName]);
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
        if (exitCode != ExitCodeType.Success)
            return;

        var directoryForArchive = SecureLocalScratchArea;

        // This archive will have directories in it
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"The remainder of the load has completed successfully, now moving all archived images to the DRS Image Archive at {ArchivePath.FullName}"));
        foreach (var dir in directoryForArchive.EnumerateDirectories())
        {
            Directory.Move(dir.FullName, Path.Combine(ArchivePath.FullName, dir.Name));
        }

        // The scratch area *should* now be empty, flag it if not for further investigation
        if (SecureLocalScratchArea.EnumerateFileSystemInfos().Any())
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"SecureLocalScratchArea is not empty - a successful load should result in an empty directory at {SecureLocalScratchArea.FullName}"));
    }

}