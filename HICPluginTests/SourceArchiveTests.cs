using System.IO;
using System.Linq;
using DrsPlugin.Attachers;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace DrsPluginTests;

public class SourceArchiveTests
{
    [Test]
    public void TestFilesystemArchiveProvider()
    {
        var rootDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var rootDir = Directory.CreateDirectory(rootDirPath);

        try
        {
            // Create some fake directories and files
            var subdir1 = rootDir.CreateSubdirectory("Subdir1");
            var subdir2 = rootDir.CreateSubdirectory("Subdir2");
                
            File.WriteAllText(Path.Combine(subdir1.FullName, "file1.txt"), "test");
            File.WriteAllText(Path.Combine(subdir1.FullName, "file2.txt"), "");
            File.WriteAllText(Path.Combine(subdir2.FullName, "file3.txt"), "");
            File.WriteAllText(Path.Combine(subdir1.FullName, "not-part-of-archive.foo"), "");

            var archiveProvider = new FilesystemArchiveProvider(rootDirPath, new[] {".txt"}, ThrowImmediatelyDataLoadEventListener.Quiet);
            var entryNames = archiveProvider.EntryNames.ToList();
                
            Assert.IsTrue(entryNames.Contains("file1.txt"));
            Assert.IsTrue(entryNames.Contains("file2.txt"));
            Assert.IsTrue(entryNames.Contains("file3.txt"));
            Assert.IsFalse(entryNames.Contains("not-part-of-archive.foo"));

            using (var file1 = archiveProvider.EntryStreams.Single(kvp => kvp.Key == "file1.txt").Value)
            {
                using (var reader = new StreamReader(file1))
                {
                    Assert.AreEqual("test", reader.ReadLine());
                }
            }
        }
        finally
        {
            rootDir.Delete(true);
        }
    }
}