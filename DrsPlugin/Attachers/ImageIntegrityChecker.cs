using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DrsPlugin.Attachers;

public class ImageIntegrityChecker
{
    public void VerifyIntegrityOfStrippedImages(IArchiveProvider archive, string pathToStrippedFiles, IDataLoadEventListener listener)
    {
        CheckImagesInOutputDirectory(archive, pathToStrippedFiles, listener);
    }

    public void VerifyIntegrityOfStrippedImages(string pathToOriginalFiles, string pathToStrippedFiles, IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Comparing images in {pathToStrippedFiles} to {pathToOriginalFiles}"));

        var patcherFactory = new CachedPatcherFactory();
        var sw = new Stopwatch();
        var imageNum = 0;
        sw.Start();
        foreach (var strippedImage in Directory.EnumerateFiles(pathToStrippedFiles))
        {
            imageNum++;
            listener.OnProgress(this,
                new ProgressEventArgs("Checking images", new ProgressMeasurement(imageNum, ProgressType.Records),
                    sw.Elapsed));
            var patcher = patcherFactory.Create(Path.GetExtension(strippedImage));

            // Find the image in the archive
            var filename = Path.GetFileName(strippedImage);
            if (filename == null)
                throw new InvalidOperationException(
                    $"Could not retrieve filename from stripped image path: {strippedImage}");

            var file = Directory.EnumerateFiles(pathToOriginalFiles, filename, SearchOption.AllDirectories).SingleOrDefault();
            if (file == null)
                throw new FileNotFoundException(
                    $"Could not find original file {strippedImage} in {pathToOriginalFiles}");

            using (var originalFileStream = File.OpenRead(file))
            {
                using (var strippedFileStream = File.OpenRead(strippedImage))
                {
                    CompareStreams(patcher, originalFileStream, strippedFileStream);
                }
            }

        }
    }

    private void CheckImagesInOutputDirectory(IArchiveProvider archive, string pathToStrippedFiles, IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Stripped images in {pathToStrippedFiles}"));

        var patcherFactory = new CachedPatcherFactory();
        var sw = new Stopwatch();
        var imageNum = 0;
        sw.Start();
        foreach (var image in Directory.EnumerateFiles(pathToStrippedFiles))
        {
            imageNum++;
            listener.OnProgress(this, new ProgressEventArgs("Checking images", new ProgressMeasurement(imageNum, ProgressType.Records), sw.Elapsed));
            var patcher = patcherFactory.Create(Path.GetExtension(image));

            // Find the image in the archive
            using (var ms = archive.GetEntry(Path.GetFileName(image)))
            {
                using (var strippedFileStream = File.OpenRead(image))
                {
                    CompareStreams(patcher, ms, strippedFileStream);
                }
            }
        }
    }

    private static void CompareStreams(IImagePatcher patcher, Stream originalImageStream, Stream strippedImageStream)
    {
        var originalPixels = patcher.ReadPixelData(originalImageStream);
        var strippedPixels = patcher.ReadPixelData(strippedImageStream);
        if (ByteArrayCompare(originalPixels, strippedPixels)) return;

        // There is an integrity issue
        string additional;
        if (originalPixels.Length == strippedPixels.Length)
            additional = "The pixel byte arrays are the same length, some of the pixel values have been changed.";
        else
            additional =
                $"The pixel byte array lengths are different. Original = {originalPixels.Length}, Stripped = {strippedPixels.Length}";

        throw new InvalidOperationException(
            $"The EXIF stripping process appears to have altered the pixel data. {additional}");
    }

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int memcmp(byte[] b1, byte[] b2, long count);

    static bool ByteArrayCompare(byte[] b1, byte[] b2)
    {
        // Validate buffers are the same length.
        // This also ensures that the count does not exceed the length of either buffer.  
        return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
    }
}