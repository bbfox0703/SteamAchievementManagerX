using System;
using System.Buffers.Binary;
using System.Drawing;
using System.IO;
using System.Net.Http;

namespace SAM.WinForms
{
    /// <summary>
    /// Provides image validation and stream reading utilities.
    /// </summary>
    public static class ImageValidator
    {
        /// <summary>
        /// Attempts to read width/height from PNG or JPEG header bytes without fully decoding.
        /// Returns false for unknown formats (caller may fall back to full decode).
        /// </summary>
        internal static bool TryGetDimensionsFromHeader(byte[] data, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (data == null || data.Length < 24)
            {
                return false;
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A, then IHDR chunk at offset 8.
            // IHDR width = bytes 16-19 (big-endian uint32), height = bytes 20-23.
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
                data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            {
                uint w = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16, 4));
                uint h = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20, 4));
                if (w > int.MaxValue || h > int.MaxValue)
                {
                    return false;
                }
                width = (int)w;
                height = (int)h;
                return true;
            }

            // JPEG: SOI = FF D8. Walk markers until a SOF (FF C0..CF except C4, C8, CC).
            if (data[0] == 0xFF && data[1] == 0xD8)
            {
                int i = 2;
                while (i + 9 < data.Length)
                {
                    if (data[i] != 0xFF)
                    {
                        return false;
                    }
                    // Skip fill bytes (FF padding)
                    while (i < data.Length && data[i] == 0xFF)
                    {
                        i++;
                    }
                    if (i >= data.Length)
                    {
                        return false;
                    }

                    byte marker = data[i++];

                    // Standalone markers (no length): SOI (D8), EOI (D9), RSTn (D0-D7)
                    if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                    {
                        continue;
                    }

                    if (i + 1 >= data.Length)
                    {
                        return false;
                    }
                    int segLen = (data[i] << 8) | data[i + 1];
                    if (segLen < 2)
                    {
                        return false;
                    }

                    // SOF0..SOF15 except SOF4 (DHT), SOF8 (reserved), SOF12 (reserved)
                    bool isSof = marker >= 0xC0 && marker <= 0xCF
                        && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                    if (isSof)
                    {
                        // SOF segment: length(2) + precision(1) + height(2) + width(2)
                        if (i + 7 >= data.Length)
                        {
                            return false;
                        }
                        height = (data[i + 3] << 8) | data[i + 4];
                        width = (data[i + 5] << 8) | data[i + 6];
                        return true;
                    }

                    i += segLen;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads data from a stream with a maximum size limit.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="maxBytes">Maximum number of bytes to read</param>
        /// <returns>Byte array containing the stream data</returns>
        /// <exception cref="HttpRequestException">Thrown when stream exceeds maxBytes</exception>
        public static byte[] ReadStreamWithLimit(Stream stream, int maxBytes)
        {
            using MemoryStream memory = new();
            byte[] buffer = new byte[81920];
            int read;
            int total = 0;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new HttpRequestException("Response exceeded maximum allowed size");
                }
                memory.Write(buffer, 0, read);
            }
            return memory.ToArray();
        }

        /// <summary>
        /// Validates that a content type is an image MIME type.
        /// </summary>
        /// <param name="contentType">The content type to validate</param>
        /// <returns>True if the content type starts with "image/", false otherwise</returns>
        public static bool IsImageContentType(string contentType)
        {
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Attempts to validate and load an image from byte data.
        /// Checks size and dimension constraints.
        /// </summary>
        /// <param name="data">The image byte data</param>
        /// <param name="maxBytes">Maximum allowed byte size</param>
        /// <param name="maxDimension">Maximum allowed width or height in pixels</param>
        /// <param name="image">Output parameter for the loaded image (null if validation fails)</param>
        /// <returns>True if image is valid and loaded successfully, false otherwise</returns>
        public static bool TryValidateAndLoadImage(byte[] data, int maxBytes, int maxDimension, out Image? image)
        {
            image = null;

            // Check size
            if (data.Length > maxBytes)
            {
                return false;
            }

            // Reject oversized dimensions *before* full decode to mitigate decompression bombs.
            // Unknown formats fall through to full decode (which will still enforce the dimension check below).
            if (TryGetDimensionsFromHeader(data, out int headerWidth, out int headerHeight))
            {
                if (headerWidth <= 0 || headerHeight <= 0
                    || headerWidth > maxDimension || headerHeight > maxDimension)
                {
                    return false;
                }
            }

            using var stream = new MemoryStream(data, false);
            try
            {
                using var tempImage = Image.FromStream(
                    stream,
                    useEmbeddedColorManagement: false,
                    validateImageData: true);

                // Check dimensions
                if (tempImage.Width > maxDimension || tempImage.Height > maxDimension)
                {
                    return false;
                }

                // Create a copy to return (tempImage will be disposed)
                image = new Bitmap(tempImage);
                return true;
            }
            catch (ArgumentException)
            {
                // Invalid image data
                return false;
            }
            catch (OutOfMemoryException)
            {
                // Image data corrupted or too large
                return false;
            }
        }

        /// <summary>
        /// Attempts to load and validate an image from a cache file.
        /// Deletes the cache file if validation fails.
        /// </summary>
        /// <param name="cachePath">Path to the cache file</param>
        /// <param name="maxBytes">Maximum allowed byte size</param>
        /// <param name="maxDimension">Maximum allowed width or height in pixels</param>
        /// <param name="image">Output parameter for the loaded image (null if validation fails)</param>
        /// <returns>True if image was loaded successfully from cache, false otherwise</returns>
        public static bool TryLoadImageFromCache(string cachePath, int maxBytes, int maxDimension, out Image? image)
        {
            image = null;

            if (!File.Exists(cachePath))
            {
                return false;
            }

            try
            {
                var bytes = File.ReadAllBytes(cachePath);

                if (TryValidateAndLoadImage(bytes, maxBytes, maxDimension, out image))
                {
                    return true;
                }

                // Validation failed, delete corrupt cache
                TryDeleteCacheFile(cachePath);
                return false;
            }
            catch (ArgumentException)
            {
                TryDeleteCacheFile(cachePath);
                return false;
            }
            catch (OutOfMemoryException)
            {
                TryDeleteCacheFile(cachePath);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void TryDeleteCacheFile(string cachePath)
        {
            try
            {
                File.Delete(cachePath);
            }
            catch
            {
                // Ignore deletion failures
            }
        }
    }
}
