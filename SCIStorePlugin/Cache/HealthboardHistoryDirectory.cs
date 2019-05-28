using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Cache
{
    public class HealthboardHistoryDirectory : IDictionary<Discipline, DirectoryInfo>
    {
        public HealthBoard Healthboard { get; set; }
        public DirectoryInfo HealthboardDirectoryInfo { get; set; }
        public Dictionary<Discipline, DirectoryInfo> DownloadDirectories { get; private set; }

        public HealthboardHistoryDirectory(HealthBoard healthboard, DirectoryInfo healthboardDirectoryInfo)
        {
            if (!healthboardDirectoryInfo.Name.Equals(healthboard.ToString()))
                throw new ArgumentException("Healthboard folder name must match healthboard");

            Healthboard = healthboard;
            HealthboardDirectoryInfo = healthboardDirectoryInfo;
            DownloadDirectories = new Dictionary<Discipline, DirectoryInfo>();

            foreach (var subdir in healthboardDirectoryInfo.EnumerateDirectories())
            {
                Discipline discipline;
                if (!Discipline.TryParse(subdir.Name, out discipline))
                    throw new Exception("Did not recognise " + subdir.Name + " as a valid discipline");

                DownloadDirectories.Add(discipline, subdir);
            }
        }

        public void CreateIfNotExists(Discipline discipline)
        {
            if (!DownloadDirectories.ContainsKey(discipline))
            {
                DirectoryInfo directoryInfo = HealthboardDirectoryInfo.CreateSubdirectory(discipline.ToString());
                DownloadDirectories.Add(discipline, directoryInfo);
            }
        }


        public void CleanupLingeringXMLFiles()
        {
            foreach (var discipline in DownloadDirectories.Keys)
                CleanupLingeringXMLFiles(discipline);
        }

        public void CleanupLingeringXMLFiles(Discipline discipline)
        {
            if (DownloadDirectories[discipline].Exists)
            {
                var toDelete = DownloadDirectories[discipline].EnumerateFiles("*.xml").ToArray();

                foreach (var file in toDelete)
                {
                    int trys = 10;
                    Retry:
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        if (trys-- == 0)
                            throw;

                        goto Retry;
                    }
                }

            }
        }

        #region  Dictionary Interface
        public IEnumerator<KeyValuePair<Discipline, DirectoryInfo>> GetEnumerator()
        {
            return DownloadDirectories.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return DownloadDirectories.GetEnumerator();
        }

        public void Add(KeyValuePair<Discipline, DirectoryInfo> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<Discipline, DirectoryInfo> item)
        {
            return DownloadDirectories.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<Discipline, DirectoryInfo>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<Discipline, DirectoryInfo> item)
        {
            throw new NotSupportedException();
        }

        public int Count { get { return DownloadDirectories.Count; } }
        public bool IsReadOnly { get { return true; } }
        public bool ContainsKey(Discipline key)
        {
            return DownloadDirectories.ContainsKey(key);
        }

        public void Add(Discipline key, DirectoryInfo value)
        {
            throw new NotSupportedException();
        }

        public bool Remove(Discipline key)
        {
            throw new NotSupportedException();
        }

        public bool TryGetValue(Discipline key, out DirectoryInfo value)
        {
            return DownloadDirectories.TryGetValue(key, out value);
        }

        public DirectoryInfo this[Discipline key]
        {
            get { return DownloadDirectories[key]; }
            set { throw new NotSupportedException(); }
        }

        public ICollection<Discipline> Keys { get { return DownloadDirectories.Keys; } }
        public ICollection<DirectoryInfo> Values { get { return DownloadDirectories.Values; } }

        #endregion

        public void Validate()
        {
            // check that the directory doesn't contain any unexpected directories
            var subdirectories = HealthboardDirectoryInfo.EnumerateDirectories().ToArray();
            if (!subdirectories.Any())
                return; // bit odd, but not strictly an error

            var recognisedDisciplines = DownloadDirectories.Keys;
            foreach (var dir in subdirectories)
            {
                Discipline discipline;
                if (!Enum.TryParse(dir.Name, out discipline))
                    throw new Exception("Unrecognised discipline: " + discipline);

                if (!recognisedDisciplines.Contains(discipline))
                    throw new Exception("HealthboardHistoryDirectory is not configured to use this discipline: " + discipline);
            }
        }
    }
}