using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DrsPlugin.Attachers
{
    public class ZipArchiveProvider : IArchiveProvider
    {
        private readonly string _archivePath;

        public ZipArchiveProvider(string archivePath)
        {
            _archivePath = archivePath;
        }

        public MemoryStream GetEntry(string entryName)
        {
            using (var fs = new FileStream(_archivePath, FileMode.Open, FileAccess.Read))
            {
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    return new MemoryStream(ReadImageBytesFromEntry(archive.Entries.Single(e => e.Name == entryName)));
                }
            }
        }

        public int GetNumEntries()
        {
            throw new NotImplementedException();
        }

        IEnumerable<KeyValuePair<string, MemoryStream>> IArchiveProvider.EntryStreams
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> EntryNames
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable EntryStreams
        {
            get { throw new NotImplementedException(); }
        }

        private byte[] ReadImageBytesFromEntry(ZipArchiveEntry entry)
        {
            var buffer = new byte[32768];
            using (var inputStream = entry.Open())
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
    }
}