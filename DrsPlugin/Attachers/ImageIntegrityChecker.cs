using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        var sw = Stopwatch.StartNew();
        var imageNum = 0;
        foreach (var strippedImage in Directory.EnumerateFiles(pathToStrippedFiles))
        {
            imageNum++;
            listener.OnProgress(this,
                new ProgressEventArgs("Checking images", new ProgressMeasurement(imageNum, ProgressType.Records),
                    sw.Elapsed));
            var patcher = CachedPatcherFactory.Create(Path.GetExtension(strippedImage));

            // Find the image in the archive
            var filename = Path.GetFileName(strippedImage);
            var file = Directory.EnumerateFiles(pathToOriginalFiles, filename, SearchOption.AllDirectories).SingleOrDefault() ?? throw new FileNotFoundException(
                    $"Could not find original file {strippedImage} in {pathToOriginalFiles}");
            using var originalFileStream = File.OpenRead(file);
            using var strippedFileStream = File.OpenRead(strippedImage);
            CompareStreams(patcher, originalFileStream, strippedFileStream);
        }
    }

    private void CheckImagesInOutputDirectory(IArchiveProvider archive, string pathToStrippedFiles, IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Stripped images in {pathToStrippedFiles}"));

        var sw = Stopwatch.StartNew();
        var imageNum = 0;
        foreach (var image in Directory.EnumerateFiles(pathToStrippedFiles))
        {
            imageNum++;
            listener.OnProgress(this, new ProgressEventArgs("Checking images", new ProgressMeasurement(imageNum, ProgressType.Records), sw.Elapsed));
            var patcher = CachedPatcherFactory.Create(Path.GetExtension(image));

            // Find the image in the archive
            using var ms = archive.GetEntry(Path.GetFileName(image));
            using var strippedFileStream = File.OpenRead(image);
            CompareStreams(patcher, ms, strippedFileStream);
        }
    }

    private static void CompareStreams(IImagePatcher patcher, Stream originalImageStream, Stream strippedImageStream)
    {
        var originalPixels = patcher.ReadPixelData(originalImageStream);
        var strippedPixels = patcher.ReadPixelData(strippedImageStream);
        if (originalPixels.SequenceEqual(strippedPixels)) return;

        // There is an integrity issue
        var additional = originalPixels.Length == strippedPixels.Length
            ? "The pixel byte arrays are the same length, some of the pixel values have been changed."
            : $"The pixel byte array lengths are different. Original = {originalPixels.Length}, Stripped = {strippedPixels.Length}";

        throw new InvalidOperationException(
            $"The EXIF stripping process appears to have altered the pixel data. {additional}");
    }
}