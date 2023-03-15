using System.Collections.Generic;
using System.IO;

namespace DrsPlugin.Attachers;

public interface IArchiveProvider
{
    MemoryStream GetEntry(string entryName);
    int GetNumEntries();
    IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams { get; }
    IEnumerable<string> EntryNames { get; }
    string Name { get; }
}