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

        public static byte[] Load(string baseDirectory, HttpClient httpClient, out bool usedLocal)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            string localPath = Path.Combine(baseDirectory, "games.xml");

            // use existing file if it was downloaded within the last 30 minutes
            if (File.Exists(localPath) == true)
            {
                DateTime lastWrite = File.GetLastWriteTimeUtc(localPath);
                if (DateTime.UtcNow - lastWrite < TimeSpan.FromMinutes(30))
                {
                    usedLocal = true;
                    return File.ReadAllBytes(localPath);
                }
            }

            byte[]? bytes = null;
            usedLocal = false;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://gib.me/sam/games.xml"));
                using var response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == null || contentLength.Value > MaxDownloadBytes)
                {
                    throw new HttpRequestException("Response too large or missing length");
                }

                using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                bytes = ReadWithLimit(stream, MaxDownloadBytes);

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
    }
}

