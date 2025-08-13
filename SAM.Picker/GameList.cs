using System;
using System.IO;
using System.Net;

namespace SAM.Picker
{
    internal static class GameList
    {
        public static byte[] Load(string baseDirectory, Func<Uri, byte[]> downloader, out bool usedLocal)
        {
            if (downloader == null)
            {
                throw new ArgumentNullException(nameof(downloader));
            }

            byte[] bytes = null;
            usedLocal = false;

            try
            {
                bytes = downloader(new Uri("https://gib.me/sam/games.xml"));
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.TrustFailure || ex.Status == WebExceptionStatus.SecureChannelFailure)
            {
                try
                {
                    bytes = downloader(new Uri("http://gib.me/sam/games.xml"));
                }
                catch
                {
                }
            }
            catch
            {
            }

            if (bytes == null)
            {
                string localPath = Path.Combine(baseDirectory, "games.xml");
                if (File.Exists(localPath) == true)
                {
                    bytes = File.ReadAllBytes(localPath);
                    usedLocal = true;
                }
            }

            if (bytes == null)
            {
                throw new InvalidOperationException("Unable to load game list from network or local file.");
            }

            return bytes;
        }
    }
}

