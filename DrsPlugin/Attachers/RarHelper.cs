using SharpCompress.Archives;
using System;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

public static class RarHelper
{
    public static void ExtractMultiVolumeArchive(DirectoryInfo sourceDir, string destinationDir = null)
    {
        ExtractMultiVolumeArchive(sourceDir.FullName, destinationDir);
    }

    public static void ExtractMultiVolumeArchive(string sourceDir, string destinationDir = null)
    {
        destinationDir ??= sourceDir;
        var firstVolume = Directory.EnumerateFiles(sourceDir, "*.rar").FirstOrDefault() ?? throw new InvalidOperationException($"No RAR files found in {sourceDir}");
        ArchiveFactory.WriteToDirectory(firstVolume, destinationDir);
    }
}