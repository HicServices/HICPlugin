using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibArchive.Net;

namespace DrsPlugin.Attachers;

public class MultiVolumeRarProvider : IArchiveProvider
{
    private readonly IDataLoadEventListener _listener;
    private readonly string[] _files;

    public MultiVolumeRarProvider(string archiveDirectory, IDataLoadEventListener listener)
    {
        _listener = listener;
        _files = Directory.EnumerateFiles(archiveDirectory, "*.rar").ToArray();
        Array.Sort(_files);
    }

    private IEnumerable<string> Entries
    {
        get
        {
            using var arc=new LibArchiveReader(_files);
            var sw = Stopwatch.StartNew();
            foreach (var e in arc.Entries())
                yield return e.Name;
            _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Listed multi-volume RAR archive contents in {sw.ElapsedMilliseconds}ms"));
        }
    }

    public MemoryStream GetEntry(string name)
    {
        using var arc = new LibArchiveReader(_files);
        foreach (var e in arc.Entries())
        {
            if (e.Name != name) continue;
            var ms = new MemoryStream();
            using var input = e.Stream;
            input.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        throw new FileNotFoundException($"Could not find {name} in archive at {_files[0]}");
    }

    private void Rewind()
    {
        _streams.ForEach(s => s.Position = 0);
    }

    public int GetNumEntries()
    {
        return Entries.Count();
    }

    public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams
    {
        get
        {
            using var arc = new LibArchiveReader(_files);
            foreach (var e in arc.Entries())
                yield return new KeyValuePair<string, MemoryStream>(e.Name, ReadImageBytesFromEntry(e.Stream));
        }
    }

    public IEnumerable<string> EntryNames => Entries;

    public string Name => $"Multi-volume archive starting {_files[0]}";

    private static MemoryStream ReadImageBytesFromEntry(Stream input)
    {
        using var inputStream = input;
        var outputStream = new MemoryStream();
        inputStream.CopyTo(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

 }