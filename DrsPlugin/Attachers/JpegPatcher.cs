/**
 * From http://www.techmikael.com/2009/07/removing-exif-data-continued.html
 * */

using System;
using System.IO;

namespace DrsPlugin.Attachers
{
    public interface IImagePatcher
    {
        Stream PatchAwayExif(Stream inStream, Stream outStream);
        byte[] ReadPixelData(Stream stream);
    }

    public class CachedPatcherFactory
    {
        private readonly JpegPatcher _jpegPatcher;
        private readonly PngPatcher _pngPatcher;

        public CachedPatcherFactory()
        {
            _jpegPatcher = new JpegPatcher();
            _pngPatcher = new PngPatcher();
        }

        public IImagePatcher Create(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return _jpegPatcher;
                case ".png":
                    return _pngPatcher;
                default:
                    throw new InvalidOperationException("Can't create a patcher for images with extension type: " + extension);
            }
        }
    }

    public class PngPatcher : IImagePatcher
    {
        public Stream PatchAwayExif(Stream inStream, Stream outStream)
        {
            int readCount;
            byte[] readBuffer = new byte[4096];
            while ((readCount = inStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                outStream.Write(readBuffer, 0, readCount);

            return outStream;
        }

        public byte[] ReadPixelData(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[4096];
                while (true)
                {
                    var numBytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (numBytesRead <= 0)
                        return ms.ToArray();

                    ms.Write(buffer, 0, numBytesRead);
                }
            }
        }
    }

    public class JpegPatcher : IImagePatcher
    {
        public byte[] ReadPixelData(Stream stream)
        {
            if (!CheckIsJpegFile(stream))
                throw new InvalidOperationException("This is not a jpeg file");

            SkipAppHeaderSection(stream);

            using (var ms = new MemoryStream())
            {
                var buffer = new byte[4096];
                while (true)
                {
                    var numBytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (numBytesRead <= 0)
                        return ms.ToArray();

                    ms.Write(buffer, 0, numBytesRead);
                }
            }
        }

        private bool CheckIsJpegFile(Stream stream)
        {
            var jpegHeader = new byte[2];
            jpegHeader[0] = (byte)stream.ReadByte();
            jpegHeader[1] = (byte)stream.ReadByte();
            return jpegHeader[0] == 0xff && jpegHeader[1] == 0xd8;
        }

        public Stream PatchAwayExif(Stream inStream, Stream outStream)
        {
            if (CheckIsJpegFile(inStream))
            {
                SkipAppHeaderSection(inStream);
            }
            outStream.WriteByte(0xff);
            outStream.WriteByte(0xd8);

            int readCount;
            byte[] readBuffer = new byte[4096];
            while ((readCount = inStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                outStream.Write(readBuffer, 0, readCount);

            return outStream;
        }

        private void SkipAppHeaderSection(Stream inStream)
        {
            byte[] header = new byte[2];
            header[0] = (byte)inStream.ReadByte();
            header[1] = (byte)inStream.ReadByte();

            while (header[0] == 0xff && (header[1] >= 0xe0 && header[1] <= 0xef))
            {
                int exifLength = inStream.ReadByte();
                exifLength = exifLength << 8;
                exifLength |= inStream.ReadByte();

                for (int i = 0; i < exifLength - 2; i++)
                {
                    inStream.ReadByte();
                }
                header[0] = (byte)inStream.ReadByte();
                header[1] = (byte)inStream.ReadByte();
            }
            inStream.Position -= 2; //skip back two bytes
        }
    }
}