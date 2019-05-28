using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DrsPlugin.Attachers
{
    public class FilesystemArchiveProvider : IArchiveProvider, IDisposable
    {
        private readonly string _rootPath;
        private readonly string[] _fileExtensions;
        private readonly IDataLoadEventListener _listener;
        private string[] _fileList;

        public FilesystemArchiveProvider(string rootPath, string[] fileExtensions, IDataLoadEventListener listener)
        {
            if (fileExtensions.Any(ext => !ext.StartsWith(".")))
                throw new ArgumentException("Please ensure the extensions start with '.', i.e. '.txt'");

            _rootPath = rootPath;
            _fileExtensions = fileExtensions;
            _listener = listener;

            Name = "What is this for again?";
        }

        private void CreateFileList()
        {
            _fileList = _fileExtensions.AsParallel()
                .SelectMany(
                    fileExtension => Directory.GetFiles(_rootPath, "*" + fileExtension, SearchOption.AllDirectories))
                .ToArray();
        }

        public MemoryStream GetEntry(string entryName)
        {
            if (_fileList == null)
                CreateFileList();
            
            // Find the file
            var entry = _fileList.SingleOrDefault(p => Path.GetFileName(p) == entryName);
            if (entry == null)
                throw new InvalidOperationException("Could not find file " + entryName + " in " + _rootPath + " or subdirectories");

            return new MemoryStream(File.ReadAllBytes(entry));
        }

        public int GetNumEntries()
        {
            if (_fileList == null)
                CreateFileList();

            return _fileList.Length;
        }

        public IEnumerable<KeyValuePair<string, MemoryStream>> EntryStreams 
        {
            get
            {
                if (_fileList == null)
                    CreateFileList();

                foreach (var path in _fileList)
                {
                    MemoryStream memoryStream;
                    try
                    {
                        memoryStream = new MemoryStream(File.ReadAllBytes(path));
                    }
                    catch (Exception e)
                    {
                        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "First attempt to load file failed: " + path + ". Will try again in 30 seconds.", e));

                        // The network is potentially temporarily unavailable, back off a bit and then retry
                        // If we fail again then just let the exception bubble up
                        Task.Delay(30000).Wait();
                        memoryStream = new MemoryStream(File.ReadAllBytes(path));
                    }

                    yield return new KeyValuePair<string, MemoryStream>(Path.GetFileName(path), memoryStream);
                }
            }
        }

        public IEnumerable<string> EntryNames 
        {
            get
            {
                if (_fileList == null)
                    CreateFileList();

                return _fileList.Select(Path.GetFileName);
            }
        }

        public string Name { get; private set; }
        public void Dispose()
        {
        }
    }
}