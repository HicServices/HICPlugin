using Rdmp.Core.ReusableLibraryCode.Progress;
using SharpCompress.Common;
using SharpCompress.Readers.Rar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers;

public class MultiVolumeRarProvider : IArchiveProvider, IDisposable
{
    private readonly string _archiveDirectory;
    private readonly IDataLoadEventListener _listener;
    private readonly List<FileStream> _streams;

    public MultiVolumeRarProvider(string archiveDirectory, IDataLoadEventListener listener)
    {
        _archiveDirectory = archiveDirectory;
        _listener = listener;

        var archiveFiles = Directory.EnumerateFiles(_archiveDirectory, "*.rar").ToList();
        _streams = archiveFiles.Select(File.OpenRead).ToList();
    }

    public IEnumerable<Entry> Entries
    {
        get
        {
            var i = 0;
            var sw = new Stopwatch();
            sw.Start();
            _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Opening multi-volume RAR archive"));
            using (var reader = RarReader.Open(_streams))
            {
                while (reader.MoveToNextEntry())
                {
                    _listener.OnProgress(this, new ProgressEventArgs("Scanning entries...", new ProgressMeasurement(i, ProgressType.Records), sw.Elapsed));
                    ++i;
                    yield return reader.Entry;
                }
            }

            Rewind();
        }
    }

    private MemoryStream GetMemoryStreamForEntry(string name)
    {
        using var reader = RarReader.Open(_streams);
        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.Key != name) continue;
            var memoryStream = new MemoryStream((int)reader.Entry.Size);
            using var inStream = reader.OpenEntryStream();
            inStream.CopyTo(memoryStream);
            Rewind();
            return memoryStream;
        }

        throw new FileNotFoundException($"Could not find {name} in archive at {_archiveDirectory}");
    }

    private void Rewind()
    {
        _streams.ForEach(s => s.Position = 0);
    }

    public MemoryStream GetEntry(string entryName)
    {
        Rewind();

        return GetMemoryStreamForEntry(entryName);
    }

    public int GetNumEntries()
    {
        var numEntries = Entries.Count();
        Rewind();
        return numEntries;
    }

    public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams
    {
        get
        {
            using var reader = RarReader.Open(_streams);
            while (reader.MoveToNextEntry())
            {
                yield return new KeyValuePair<string, MemoryStream>(reader.Entry.Key, ReadImageBytesFromEntry(reader));
            }

            Rewind();
        }
    }

    public IEnumerable<string> EntryNames => Entries.Select(e => e.Key);

    public string Name => $"Multi-volume archive at {_archiveDirectory}";

    private static MemoryStream ReadImageBytesFromEntry(RarReader reader)
    {
        using var inputStream = reader.OpenEntryStream();
        var outputStream = new MemoryStream((int)reader.Entry.Size);
        inputStream.CopyTo(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _streams.ForEach(s => s.Dispose());
    }
}