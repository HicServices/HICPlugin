using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DrsPlugin.Attachers;

public class ZipArchiveProvider : IArchiveProvider
{
    private readonly string _archivePath;

    public ZipArchiveProvider(string archivePath)
    {
        _archivePath = archivePath;
    }

    public MemoryStream GetEntry(string entryName)
    {
        using var fs = new FileStream(_archivePath, FileMode.Open, FileAccess.Read);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
        return ReadImageBytesFromEntry(archive.Entries.Single(e => e.Name == entryName));
    }

    public int GetNumEntries() => throw new NotImplementedException();

    IEnumerable<KeyValuePair<string, MemoryStream>> IArchiveProvider.EntryStreams => throw new NotImplementedException();

    public IEnumerable<string> EntryNames => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();

    private static MemoryStream ReadImageBytesFromEntry(ZipArchiveEntry entry)
    {
        using var inputStream = entry.Open();
        var outputStream = new MemoryStream();
        inputStream.CopyTo(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }
}