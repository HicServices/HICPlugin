using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DrsPlugin.Extraction
{
    /// <summary>
    /// This class wraps functionality for interacting with the image archive used across loading and extraction
    /// </summary>
    public class ImageArchiveRepository
    {
        private readonly string _archiveRoot;

        public ImageArchiveRepository(string archiveRoot)
        {
            _archiveRoot = archiveRoot;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="archiveUri"></param>
        /// <param name="outputStream">Caller is reponsible for opening and disposing of outputStream</param>
        public void CopyToStream(string archiveUri, Stream outputStream)
        {
            if (outputStream == null)
                throw new ArgumentException("Must pass in a valid open output stream.", "outputStream");

            ValidateUriString(archiveUri);

            var uriParts = archiveUri.Split('!');
            var archiveLocation = Path.Combine(_archiveRoot, uriParts[0]);
            if (!File.Exists(archiveLocation))
                throw new InvalidOperationException("The archive " + uriParts[0] + " does not exist at the expected location of " + archiveLocation);

            // Is this a zip or tar archive?
            var archiveType = Path.GetExtension(uriParts[0]);
            if (archiveType == ".zip")
                CopyToStreamFromZip(archiveLocation, uriParts[1], outputStream);
            else if (archiveType == ".tar")
                CopyToStreamFromTar(archiveLocation, uriParts[1], outputStream);
            else
                throw new InvalidOperationException("Unsupported type of image archive: '" + archiveType + "'. Image archives must either be .zip or .tar files.");
        }

        private void CopyToStreamFromZip(string archiveLocation, string fileName, Stream outputStream)
        {
            using (var archive = ZipFile.Open(archiveLocation, ZipArchiveMode.Read))
            {
                var entry = archive.Entries.SingleOrDefault(e => e.Name == fileName);
                if (entry == null)
                    throw new InvalidOperationException("The archive " + archiveLocation + " does not contain the file " + fileName);

                using (var entryStream = entry.Open())
                {
                    entryStream.CopyTo(outputStream);
                }
            }
        }

        private void CopyToStreamFromTar(string archiveLocation, string fileName, Stream outputStream)
        {
            using (var fs = File.OpenRead(archiveLocation))
            {
                using (var tarStream = new TarInputStream(fs))
                {
                    TarEntry entry;
                    while ((entry = tarStream.GetNextEntry()) != null)
                    {
                        if (entry.Name == fileName)
                            break;
                    }

                    if (entry == null)
                        throw new InvalidOperationException("The archive " + archiveLocation + " does not contain the file " + fileName);

                    tarStream.CopyEntryContents(outputStream);
                }
            }
            
        }

        private void ValidateUriString(string uri)
        {
            if (!uri.Contains("!"))
                throw new InvalidOperationException(uri +
                                                    " is not a valid RDMP Image Archive file URI (should be archive_filename!image_filename)");

            var parts = uri.Split(new[] { '!' });
            if (parts.Length != 2)
                throw new InvalidOperationException(uri +
                                                    " is not a valid RDMP Image Archive file URI (should be archive_filename!image_filename)");
        }

        /// <summary>
        /// Extracts a set of images from one archive. Looks for entries named after the keys in extractionMap and saves them to the path given in the map's corresponding value.
        /// </summary>
        /// <param name="archiveFilePath">Path to the file from which to extract the images</param>
        /// <param name="extractionMap">Map of entry names in the archive to full output path</param>
        public void ExtractImageSetFromTar(string archiveFilePath, Dictionary<string, string> extractionMap)
        {
            // Create a HashSet of the entry names, we'll be doing a lot of membership testing
            var entryNames = new HashSet<string>(extractionMap.Keys);
            
            using (var fs = File.OpenRead(archiveFilePath))
            {
                using (var tarStream = new TarInputStream(fs))
                {
                    TarEntry entry;
                    while ((entry = tarStream.GetNextEntry()) != null)
                    {
                        if (!entryNames.Contains(entry.Name)) continue;

                        using (var outputStream = new FileStream(extractionMap[entry.Name], FileMode.CreateNew))
                        {
                            tarStream.CopyEntryContents(outputStream);
                        }

                        entryNames.Remove(entry.Name);

                        if (entryNames.Count == 0)
                            return;
                    }
                }
            }

            // Check that we found all the entries
            if (entryNames.Count == 0)
                return;

            throw new InvalidOperationException("The following entries were not found in " + archiveFilePath + ": " + string.Join(", ", entryNames));
        }

        /// <summary>
        /// Extracts a set of images from one archive. Looks for entries named after the keys in extractionMap and saves them to the path given in the map's corresponding value.
        /// </summary>
        /// <param name="archiveFilePath">Path to the file from which to extract the images</param>
        /// <param name="extractionMap">Map of entry names in the archive to full output path</param>
        public void ExtractImageSetFromZip(string archiveFilePath, Dictionary<string, string> extractionMap)
        {
            // Create a HashSet of the entry names, we'll be doing a lot of membership testing
            var entryNames = new HashSet<string>(extractionMap.Keys);

            using (var archive = ZipFile.Open(archiveFilePath, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries.Where(entry => entryNames.Contains(entry.Name)))
                {
                    using (var entryStream = entry.Open())
                    {
                        using (var outputStream = new FileStream(extractionMap[entry.Name], FileMode.CreateNew))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }

                    entryNames.Remove(entry.Name);

                    if (entryNames.Count == 0)
                        return;
                }
            }

            // Check that we found all the entries
            if (entryNames.Count == 0)
                return;

            throw new InvalidOperationException("The following entries were not found in " + archiveFilePath + ": " + string.Join(", ", entryNames));
        }
    
    }
}