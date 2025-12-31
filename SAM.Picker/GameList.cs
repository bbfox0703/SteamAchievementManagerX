#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace SAM.Picker
{
    internal static class GameList
    {
        // Maximum allowed size for the games.xml download.
        internal const int MaxDownloadBytes = 4 * 1024 * 1024; // 4 MB

        // Cache expiration time in minutes
        private const int CacheExpirationMinutes = 30;

        // Buffer size for stream reading
        private const int StreamReadBufferSize = 81920; // 80 KB

        public static byte[] Load(string baseDirectory, HttpClient httpClient, out bool usedLocal)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            string localPath = Path.Combine(baseDirectory, "games.xml");

            // use existing file if it was downloaded within the last cache expiration time
            if (File.Exists(localPath) == true)
            {
                DateTime lastWrite = File.GetLastWriteTimeUtc(localPath);
                if (DateTime.UtcNow - lastWrite < TimeSpan.FromMinutes(CacheExpirationMinutes))
                {
                    usedLocal = true;
                    return File.ReadAllBytes(localPath);
                }
            }

            byte[]? bytes = null;
            usedLocal = false;

            try
            {
                // Use Task.Run to avoid deadlock when blocking on async operations
                bytes = System.Threading.Tasks.Task.Run(async () =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://gib.me/sam/games.xml"));
                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    if (contentLength == null || contentLength.Value > MaxDownloadBytes)
                    {
                        throw new HttpRequestException("Response too large or missing length");
                    }

                    using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    return ReadWithLimit(stream, MaxDownloadBytes);
                }).GetAwaiter().GetResult();

                if (bytes != null)
                {
                    // ensure the downloaded data is valid XML
                    try
                    {
                        using var ms = new MemoryStream(bytes, false);
                        XmlReaderSettings settings = new()
                        {
                            DtdProcessing = DtdProcessing.Prohibit,
                            XmlResolver = null,
                        };
                        using XmlReader reader = XmlReader.Create(ms, settings);
                        _ = XDocument.Load(reader, LoadOptions.SetLineInfo);
                    }
                    catch (Exception)
                    {
                        throw new InvalidDataException("Downloaded game list is invalid XML");
                    }
                }
            }
            catch (Exception)
            {
                bytes = null;
            }

            if (bytes != null)
            {
                try
                {
                    string backupPath = localPath + ".bak";
                    if (File.Exists(backupPath) == true)
                    {
                        File.Delete(backupPath);
                    }

                    if (File.Exists(localPath) == true)
                    {
                        File.Move(localPath, backupPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(baseDirectory);
                    }

                    File.WriteAllBytes(localPath, bytes);
                }
                catch
                {
                }

                return bytes;
            }

            if (File.Exists(localPath) == true)
            {
                bytes = File.ReadAllBytes(localPath);
                usedLocal = true;
            }

            if (bytes == null)
            {
                throw new InvalidOperationException("Unable to load game list from network or local file.");
            }

            return bytes;
        }

        private static byte[] ReadWithLimit(Stream stream, int maxBytes)
        {
            using MemoryStream memory = new();
            byte[] buffer = new byte[StreamReadBufferSize];
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
    }
}

