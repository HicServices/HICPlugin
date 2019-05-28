using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Cache
{
    public class RootHistoryDirectory : IDictionary<HealthBoard, HealthboardHistoryDirectory>
    {
        public DirectoryInfo Root { get; private set; }
        public Dictionary<HealthBoard, HealthboardHistoryDirectory> HealthBoardDirectories { get; private set; }

        public DirectoryInfo ErrorDirectory
        {
            get
            {
                var fi = new DirectoryInfo(Path.Combine(Root.FullName, "Errors"));
                if (fi.Exists) return fi;

                fi.Create();
                fi.Refresh();
                return fi;
            }
        }

        public RootHistoryDirectory(DirectoryInfo root)
        {
            Root = root;
            HealthBoardDirectories = new Dictionary<HealthBoard, HealthboardHistoryDirectory>();

            foreach (var dir in Root.EnumerateDirectories())
            {
                if (dir.Name.Equals(ErrorDirectory.Name))
                    continue;

                HealthBoard hb;
                if (!HealthBoard.TryParse(dir.Name, out hb))
                    throw new Exception("Did not recognise " + dir.Name + " as a valid health board");

                HealthBoardDirectories.Add(hb, new HealthboardHistoryDirectory(hb, dir));
            }

        }

        public void CreateIfNotExists(HealthBoard healthBoard, Discipline discipline)
        {
            if (!HealthBoardDirectories.ContainsKey(healthBoard))
            {
                //create hb folder
                var healthBoardDir = Root.CreateSubdirectory(healthBoard.ToString());
                var toAdd = new HealthboardHistoryDirectory(healthBoard, healthBoardDir);
                HealthBoardDirectories.Add(healthBoard, toAdd);
            }

            HealthBoardDirectories[healthBoard].CreateIfNotExists(discipline);
        }

        public DateTime? GetMostRecentDateLoaded(HealthBoard healthBoard, Discipline discipline)
        {
            CreateIfNotExists(healthBoard, discipline);



            if (!HealthBoardDirectories[healthBoard][discipline].EnumerateFiles().Any())
                return null;
            else
            {
                var mostRecentZipFile = HealthBoardDirectories[healthBoard][discipline].EnumerateFiles("*.zip").OrderByDescending(info => info.Name).FirstOrDefault();
                if (mostRecentZipFile == null)
                    throw new Exception("Directory contains dirty stuff (" + HealthBoardDirectories[healthBoard][discipline].FullName + ")");

                return DateTime.ParseExact(mostRecentZipFile.Name.Substring(0, "yyyy-MM-dd".Length), "yyyy-MM-dd", null);
            }
        }

        public void CleanupLingeringXMLFiles()
        {
            foreach (var hbDir in HealthBoardDirectories.Values)
            {
                hbDir.CleanupLingeringXMLFiles();
            }
        }

        public IEnumerator<KeyValuePair<HealthBoard, HealthboardHistoryDirectory>> GetEnumerator()
        {
            return HealthBoardDirectories.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return HealthBoardDirectories.GetEnumerator();
        }

        public void Add(KeyValuePair<HealthBoard, HealthboardHistoryDirectory> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<HealthBoard, HealthboardHistoryDirectory> item)
        {
            return HealthBoardDirectories.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<HealthBoard, HealthboardHistoryDirectory>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<HealthBoard, HealthboardHistoryDirectory> item)
        {
            throw new NotSupportedException();
        }

        public int Count { get { return HealthBoardDirectories.Count; } }
        public bool IsReadOnly { get { return true; } }
        public bool ContainsKey(HealthBoard key)
        {
            return HealthBoardDirectories.ContainsKey(key);
        }

        public void Add(HealthBoard key, HealthboardHistoryDirectory value)
        {
            throw new NotSupportedException();

        }

        public bool Remove(HealthBoard key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(HealthBoard key, out HealthboardHistoryDirectory value)
        {
            return HealthBoardDirectories.TryGetValue(key, out value);
        }

        public HealthboardHistoryDirectory this[HealthBoard key]
        {
            get { return HealthBoardDirectories[key]; }
            set { throw new NotSupportedException(); }
        }

        public ICollection<HealthBoard> Keys { get { return HealthBoardDirectories.Keys; } }
        public ICollection<HealthboardHistoryDirectory> Values { get { return HealthBoardDirectories.Values; } }

        public void Validate()
        {
            var healthBoards = HealthBoardDirectories.Keys;
            foreach (var path in Root.EnumerateDirectories())
            {
                HealthBoard healthBoard;
                if (!Enum.TryParse(path.Name, out healthBoard))
                    throw new Exception("Unknown HealthBoard subdirectory: " + path.Name);

                if (healthBoards.Contains(healthBoard))
                    throw new Exception("RootHistoryDirectory is not configured to support this HealthBoard: " + healthBoard);

                HealthBoardDirectories[healthBoard].Validate();
            }
        }
    }
}