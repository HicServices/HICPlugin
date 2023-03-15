using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using System;
using System.IO;

namespace DrsPlugin.Attachers;

public class RarHelper
{
    public void ExtractMultiVolumeArchive(DirectoryInfo sourceDir, string destDir = null)
    {
        ExtractMultiVolumeArchive(sourceDir.FullName, destDir);
    }

    private string FindFirstVolume(string sourceDir)
    {
        foreach (var archive in Directory.EnumerateFiles(sourceDir, "*.rar"))
        {
            using (var file = RarArchive.Open(archive))
            {
                if (file.IsFirstVolume())
                    return archive;
            }
        }

        throw new InvalidOperationException(
            $"No archive files in this directory identify as the first volume in a multi-volume archive: {sourceDir}");
    }

    public void ExtractMultiVolumeArchive(string sourceDir, string destDir = null)
    {
        destDir = destDir ?? sourceDir;

        var firstVolume = FindFirstVolume(sourceDir);
        ArchiveFactory.WriteToDirectory(firstVolume, destDir);
    }
}