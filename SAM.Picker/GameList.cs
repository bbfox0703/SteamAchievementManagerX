#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using SAM.API;
using SAM.API.Constants;
using SAM.API.Utilities;

namespace SAM.Picker
{
    internal static class GameList
    {
        // Cache expiration time in minutes
        private const int CacheExpirationMinutes = 30;

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
                    if (contentLength == null || contentLength.Value > DownloadLimits.MaxGameListBytes)
                    {
                        throw new HttpRequestException("Response too large or missing length");
                    }

                    using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    return StreamHelper.ReadWithLimit(stream, DownloadLimits.MaxGameListBytes);
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
            catch (Exception ex)
            {
                DebugLogger.LogWarning($"Failed to download game list from network: {ex.Message}");
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
    }
}

