using System;
using System.IO;
using System.Net;
using System.Net.Security;

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

            byte[] bytes = null;
            usedLocal = false;

            try
            {
                RemoteCertificateValidationCallback previousCallback = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
                try
                {
                    bytes = downloader(new Uri("https://gib.me/sam/games.xml"));
                }
                finally
                {
                    ServicePointManager.ServerCertificateValidationCallback = previousCallback;
                }
            }
            catch
            {
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

