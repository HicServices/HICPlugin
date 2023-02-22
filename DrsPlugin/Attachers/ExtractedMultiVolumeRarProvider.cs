using ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

/// <summary>
/// This version extracts the files from the archive first - should provide performance benefits for large archives
/// </summary>
public class ExtractedMultiVolumeRarProvider : IArchiveProvider, IDisposable
{
    private readonly string _archiveDirectory;
    private string _fileDirectory;
    private readonly IDataLoadEventListener _listener;

    private bool _isExtracted;
    private bool _doNotDeleteFileDirectory;

    public ExtractedMultiVolumeRarProvider(string archiveDirectory, IDataLoadEventListener listener)
    {
        _archiveDirectory = archiveDirectory;
        _listener = listener;
    }

    public void UseExistingExtractionDirectory(string fileDirectory)
    {
        _fileDirectory = fileDirectory;
        if (!Directory.Exists(_fileDirectory))
            throw new InvalidOperationException($"The directory {_fileDirectory} does not exist.");

        _isExtracted = true;
        _doNotDeleteFileDirectory = true; // would be a bit anti-social to remove a directory that we don't control the lifecycle of
    }

    private void ExtractFiles()
    {
        if (_isExtracted) return;

        _fileDirectory = Path.Combine(_archiveDirectory, Path.GetRandomFileName());
        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Creating a temporary directory for archive extraction: {_fileDirectory}"));
        Directory.CreateDirectory(_fileDirectory);

        var rarHelper = new RarHelper();
        rarHelper.ExtractMultiVolumeArchive(_archiveDirectory, _fileDirectory);
        _isExtracted = true;
    }

    public MemoryStream GetEntry(string entryName)
    {
        if (!_isExtracted)
            ExtractFiles();

        var filepath = Path.Combine(_fileDirectory, entryName);
        if (!File.Exists(filepath))
            throw new InvalidOperationException($"Could not find the entry: {entryName}");

        using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
        {
            return new MemoryStream(ReadBytesFromStream(fs));
        }
    }

    private static byte[] ReadBytesFromStream(Stream stream)
    {
        var buffer = new byte[32768];
        using (var outputStream = new MemoryStream())
        {
            while (true)
            {
                var numBytesRead = stream.Read(buffer, 0, buffer.Length);
                if (numBytesRead <= 0)
                    return outputStream.ToArray();
                outputStream.Write(buffer, 0, numBytesRead);
            }
        }
    }

    public int GetNumEntries()
    {
        if (!_isExtracted)
            ExtractFiles();

        return Directory.EnumerateFiles(_fileDirectory).Count();
    }

    public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams
    {
        get
        {
            if (!_isExtracted)
                ExtractFiles();

            foreach (var entryName in Directory.EnumerateFiles(_fileDirectory).Select(Path.GetFileName))
            {
                yield return new KeyValuePair<string, MemoryStream>(entryName, GetEntry(entryName));
            }
        }
    }

    public IEnumerable<string> EntryNames 
    {
        get
        {
            if (!_isExtracted)
                ExtractFiles();

            return Directory.EnumerateFiles(_fileDirectory);
        }
    }
        
    public string Name { get { return $"Multi-volume archive at {_archiveDirectory}"; } }
        
    public void Dispose()
    {
        if (!_doNotDeleteFileDirectory)
            Directory.Delete(_fileDirectory, true);

        _isExtracted = false;
    }
}