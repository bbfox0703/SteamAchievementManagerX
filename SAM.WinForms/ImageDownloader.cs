using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.WinForms
{
    /// <summary>
    /// Provides utilities for downloading and processing images via HTTP.
    /// </summary>
    public static class ImageDownloader
    {
        /// <summary>
        /// Downloads image data from a URI with size validation.
        /// </summary>
        /// <param name="uri">The URI to download from</param>
        /// <param name="httpClient">The HttpClient instance to use</param>
        /// <param name="maxBytes">Maximum allowed response size in bytes</param>
        /// <param name="cancellationToken">Token used to abort an in-flight request</param>
        /// <returns>A tuple containing the image data and content type</returns>
        /// <exception cref="HttpRequestException">Thrown when the response is too large or Content-Length is missing</exception>
        public static async Task<(byte[] Data, string ContentType)> DownloadImageDataAsync(
            Uri uri,
            HttpClient httpClient,
            int maxBytes,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength != null && contentLength.Value > maxBytes)
            {
                throw new HttpRequestException("Response too large");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // Read the body with the token: HttpClient.Timeout does not cover the
            // response stream under ResponseHeadersRead, and a synchronous read can't
            // be cancelled -- a stalled connection would otherwise block forever and
            // hang form close. This honors cancellation immediately.
            var data = await ReadWithLimitAsync(stream, maxBytes, cancellationToken);

            return (data, contentType);
        }

        private static async Task<byte[]> ReadWithLimitAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
        {
            using var memory = new MemoryStream();
            byte[] buffer = new byte[81920];
            int total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
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
        /// Downloads and validates an image, returning a Bitmap if successful.
        /// Validates content type, size, and dimensions.
        /// </summary>
        /// <param name="uri">The URI to download from</param>
        /// <param name="httpClient">The HttpClient instance to use</param>
        /// <param name="maxBytes">Maximum allowed response size in bytes</param>
        /// <param name="maxDimension">Maximum allowed width or height in pixels</param>
        /// <returns>A Bitmap if download and validation successful, null otherwise</returns>
        public static async Task<Bitmap?> DownloadAndValidateImageAsync(
            Uri uri,
            HttpClient httpClient,
            int maxBytes,
            int maxDimension)
        {
            try
            {
                var (data, contentType) = await DownloadImageDataAsync(uri, httpClient, maxBytes);

                if (!ImageValidator.IsImageContentType(contentType))
                {
                    return null;
                }

                if (ImageValidator.TryValidateAndLoadImage(data, maxBytes, maxDimension, out var image))
                {
                    return image as Bitmap ?? new Bitmap(image!);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves image bytes to a cache file.
        /// </summary>
        /// <param name="cachePath">Path to the cache file</param>
        /// <param name="data">Image byte data to save</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        public static bool TrySaveToCache(string cachePath, byte[] data)
        {
            try
            {
                File.WriteAllBytes(cachePath, data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
