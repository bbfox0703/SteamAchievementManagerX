using System;
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
