using ICSharpCode.SharpZipLib.Tar;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

/// <summary>
/// This is designed to be called multiple times, each time creating a new set of archives consecutively numbered following on from the previous call
/// </summary>
public class ImageArchiveProcessor
{
    private readonly DirectoryInfo _sourceDir;
    private readonly DirectoryInfo _destinationDir;
    private readonly int _jobId;

    private int _numArchives;

    public ImageArchiveProcessor(DirectoryInfo sourceDir, DirectoryInfo destinationDir, int jobId)
    {
        _sourceDir = sourceDir;
        _destinationDir = destinationDir;
        _jobId = jobId;
        _numArchives = 1;
    }

    public Dictionary<string, List<string>> ArchiveImagesForStorage(IDataLoadEventListener listener, int maxUncompressedSize = 0)
    {
        var archiveMappings = new Dictionary<string, List<string>>();

        var filesToArchive = _sourceDir.EnumerateFiles().ToList();
        var archiveStub = Path.Combine(_jobId.ToString(), $"{_jobId}_");

        var i = 0;
        var sw = new Stopwatch();
        sw.Start();
        listener.OnProgress(this, new ProgressEventArgs($"Archiving {filesToArchive.Count} images", new ProgressMeasurement(0, ProgressType.Records), sw.Elapsed));
        while (i < filesToArchive.Count)
        {
            // Create name for next archive
            var relativeArchivePath = $"{archiveStub}{_numArchives}.tar";
            var archiveFilepath = Path.Combine(_destinationDir.FullName, relativeArchivePath);

            var dirName = Path.GetDirectoryName(archiveFilepath) ?? throw new InvalidOperationException($"Something wrong with the path for {archiveFilepath}");
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            // Add it to the mapping table so clients know which images are in what archive
            archiveMappings.Add(relativeArchivePath, new List<string>());

            // Create a new archive
            using (var fs = new FileStream(archiveFilepath, FileMode.CreateNew, FileAccess.Write))
            {
                using var archive = TarArchive.CreateOutputTarArchive(fs);
                long totalSize = 0;

                // Add files to it while there are still files left and where the size is within the max uncompressed size
                // If maxUncompressedSize is zero then we only create one archive
                while ((maxUncompressedSize == 0 || totalSize < maxUncompressedSize) && i < filesToArchive.Count)
                {
                    var tarEntry = TarEntry.CreateEntryFromFile(filesToArchive[i].FullName);
                    tarEntry.Name = filesToArchive[i].Name;
                    archive.WriteEntry(tarEntry, false);

                    archiveMappings[relativeArchivePath].Add(filesToArchive[i].Name);

                    File.Delete(filesToArchive[i].FullName);
                    totalSize += filesToArchive[i].Length;
                    i++;
                    listener.OnProgress(this, new ProgressEventArgs($"Archiving {filesToArchive.Count} images", new ProgressMeasurement(i, ProgressType.Records), sw.Elapsed));
                }
            }

            _numArchives++;
        }

        return archiveMappings;
    }
}