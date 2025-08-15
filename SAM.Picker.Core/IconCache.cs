using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SAM.Picker.Core
{
    public class IconCache
    {
        private readonly HttpClient _httpClient;
        private readonly string _directory;
        private readonly bool _useCache;

        private const int MaxLogoBytes = 512 * 1024; // 512 KB
        private const int MaxLogoDimension = 1024; // px

        public IconCache(HttpClient httpClient, string directory)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            try
            {
                Directory.CreateDirectory(_directory);
                _useCache = true;
            }
            catch
            {
                _useCache = false;
            }
        }

        public async Task<byte[]?> GetOrDownloadAsync(uint id, Uri uri)
        {
            string? cacheFile = _useCache ? Path.Combine(_directory, id + ".png") : null;

            if (cacheFile != null && File.Exists(cacheFile))
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(cacheFile);
                    if (bytes.Length <= MaxLogoBytes && Validate(bytes))
                    {
                        return bytes;
                    }
                }
                catch
                {
                    try { if (cacheFile != null) File.Delete(cacheFile); } catch { }
                }
            }

            var (data, contentType) = await DownloadDataAsync(uri);
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (Validate(data) == false)
            {
                return null;
            }

            if (cacheFile != null)
            {
                try { await File.WriteAllBytesAsync(cacheFile, data); } catch { }
            }

            return data;
        }

        private bool Validate(byte[] data)
        {
            try
            {
                using var stream = new MemoryStream(data, false);
                using var image = Image.FromStream(stream, false, true);
                return image.Width <= MaxLogoDimension && image.Height <= MaxLogoDimension;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(byte[] Data, string ContentType)> DownloadDataAsync(Uri uri)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength == null || contentLength.Value > MaxLogoBytes)
            {
                throw new HttpRequestException("Response too large or missing length");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            using var stream = await response.Content.ReadAsStreamAsync();
            var data = await ReadWithLimitAsync(stream, MaxLogoBytes);
            return (data, contentType);
        }

        private static async Task<byte[]> ReadWithLimitAsync(Stream stream, int maxBytes)
        {
            using MemoryStream memory = new();
            byte[] buffer = new byte[81920];
            int read;
            int total = 0;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new HttpRequestException("Response exceeded maximum allowed size");
                }
                await memory.WriteAsync(buffer.AsMemory(0, read));
            }
            return memory.ToArray();
        }
    }
}
