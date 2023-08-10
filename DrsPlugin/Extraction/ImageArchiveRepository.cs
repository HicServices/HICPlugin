using System;
using System.Collections.Generic;
using System.IO;

namespace DrsPlugin.Extraction;

/// <summary>
/// This class wraps functionality for interacting with the image archive used across loading and extraction
/// </summary>
public static class ImageArchiveRepository
{
    /// <summary>
    /// Extracts a set of images from one archive. Looks for entries named after the keys in extractionMap and saves them to the path given in the map's corresponding value.
    /// </summary>
    /// <param name="archiveFilePath">Path to the file from which to extract the images</param>
    /// <param name="extractionMap">Map of entry names in the archive to full output path</param>
    public static void ExtractImageSetFromArchive(string archiveFilePath, Dictionary<string, string> extractionMap)
    {
        using var archive = new LibArchive.Net.LibArchiveReader(archiveFilePath);
        foreach(var entry in archive.Entries())
        {
            if (extractionMap.TryGetValue(entry.Name,out var destination))
            {
                using var outputStream = new FileStream(destination, FileMode.CreateNew);
                using var source=entry.Stream;
                source.CopyTo(outputStream);
                extractionMap.Remove(entry.Name);
            }
            if (extractionMap.Count == 0)
                return;
        }
        // Check that we found all the entries
        if (extractionMap.Count != 0)
            throw new InvalidOperationException(
                $"The following entries were not found in {archiveFilePath}: {string.Join(", ", extractionMap.Keys)}");
    }
}