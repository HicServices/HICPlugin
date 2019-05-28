using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using SharpCompress.Common;
using SharpCompress.Readers.Rar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DrsPlugin.Attachers
{
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

        private MemoryStream GetMemoryStreamForEntry(IEntry entryToFind)
        {
            MemoryStream memoryStream  = null;
            using (var reader = RarReader.Open(_streams))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key == entryToFind.Key)
                    {
                        memoryStream = new MemoryStream(ReadImageBytesFromEntry(reader));
                    }
                }
            }

            if (memoryStream == null)
                throw new InvalidOperationException("Could not find " + entryToFind + " in archive at " + _archiveDirectory);

            Rewind();

            return memoryStream;
        }

        private void Rewind()
        {
            _streams.ForEach(s => s.Position = 0);
        }

        public MemoryStream GetEntry(string entryName)
        {
            var entry = Entries.SingleOrDefault(e => e.Key == entryName);
            if (entry == null)
                throw new FileNotFoundException("Could not find entry in the archive at " + _archiveDirectory + " which matches '" + entryName + "'");

            Rewind();

            return GetMemoryStreamForEntry(entry);
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
                using (var reader = RarReader.Open(_streams))
                {
                    while (reader.MoveToNextEntry())
                    {
                        yield return new KeyValuePair<string, MemoryStream>(reader.Entry.Key, new MemoryStream(ReadImageBytesFromEntry(reader)));
                    }
                }

                Rewind();
            }
        }

        public IEnumerable<string> EntryNames
        {
            get { return Entries.Select(e => e.Key); }
        }

        public string Name { get { return "Multi-volume archive at " + _archiveDirectory; } }

        private byte[] ReadImageBytesFromEntry(RarReader reader)
        {
            var buffer = new byte[32768];
            using (var inputStream = reader.OpenEntryStream())
            {
                using (var outputStream = new MemoryStream())
                {
                    while (true)
                    {
                        var numBytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        if (numBytesRead <= 0)
                            return outputStream.ToArray();
                        outputStream.Write(buffer, 0, numBytesRead);
                    }
                }
            }
        }

        public void Dispose()
        {
            _streams.ForEach(s => s.Dispose());
        }
    }
}