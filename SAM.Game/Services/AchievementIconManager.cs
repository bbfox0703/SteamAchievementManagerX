using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SAM.Game.Stats;
using static SAM.Game.InvariantShorthand;

namespace SAM.Game.Services
{
    /// <summary>
    /// Manages achievement icon downloading, caching, and queue processing.
    /// </summary>
    internal class AchievementIconManager
    {
        private readonly long _gameId;
        private readonly string _iconCacheDirectory;
        private readonly List<AchievementInfo> _iconQueue;
        private bool _useIconCache;

        private const int MaxIconBytes = 512 * 1024; // 512 KB
        private const int MaxIconDimension = 1024; // px

        // Regex for validating icon filenames (must be alphanumeric + underscore + dot)
        private static readonly Regex IconNameRegex = new(@"^[a-zA-Z0-9_\.]+$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the number of icons pending in the download queue.
        /// </summary>
        public int QueueCount => this._iconQueue.Count;

        /// <summary>
        /// Initializes a new instance of the AchievementIconManager.
        /// </summary>
        /// <param name="gameId">The Steam game ID</param>
        /// <param name="iconCacheDirectory">Directory path for caching icons</param>
        /// <param name="useIconCache">Whether to use icon caching</param>
        public AchievementIconManager(long gameId, string iconCacheDirectory, bool useIconCache)
        {
            this._gameId = gameId;
            this._iconCacheDirectory = iconCacheDirectory;
            this._useIconCache = useIconCache;
            this._iconQueue = new List<AchievementInfo>();
        }

        /// <summary>
        /// Gets or sets whether icon caching is enabled.
        /// </summary>
        public bool UseIconCache
        {
            get => this._useIconCache;
            set => this._useIconCache = value;
        }

        /// <summary>
        /// Generates the cache file path for an achievement icon.
        /// </summary>
        /// <param name="info">The achievement info</param>
        /// <returns>Cache file path, or null if caching is disabled or icon name is invalid</returns>
        public string? GetCachePath(AchievementInfo info)
        {
            if (!this._useIconCache)
            {
                return null;
            }

            var icon = info.IsAchieved ? info.IconNormal : info.IconLocked;
            if (string.IsNullOrEmpty(icon))
            {
                return null;
            }

            var id = info.Id;
            var invalid = Path.GetInvalidFileNameChars();
            if (id.IndexOfAny(invalid) >= 0 ||
                id.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                id.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                var originalId = id;
                id = Uri.EscapeDataString(id);
                API.DebugLogger.Log($"Renaming icon id '{originalId}' to '{id}' for cache file name.");
            }

            var fileName = id + "_" + (info.IsAchieved ? "achieved" : "locked") + ".png";
            var path = Path.Combine(this._iconCacheDirectory, fileName);
            API.DebugLogger.Log($"Cache path for icon '{info.Id}' resolved to '{path}'.");
            return path;
        }

        /// <summary>
        /// Attempts to load an achievement icon from cache.
        /// </summary>
        /// <param name="info">The achievement info</param>
        /// <param name="icon">Output parameter for the loaded icon</param>
        /// <returns>True if icon was loaded from cache, false otherwise</returns>
        public bool TryLoadFromCache(AchievementInfo info, out Image? icon)
        {
            icon = null;

            if (!this._useIconCache)
            {
                return false;
            }

            var cachePath = this.GetCachePath(info);
            if (cachePath == null)
            {
                return false;
            }

            try
            {
                API.DebugLogger.Log($"Checking cache for icon '{info.Id}' at '{cachePath}'.");
                if (WinForms.ImageValidator.TryLoadImageFromCache(cachePath, MaxIconBytes, MaxIconDimension, out icon))
                {
                    API.DebugLogger.Log($"Loaded icon '{info.Id}' from cache.");
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                this._useIconCache = false;
                return false;
            }
        }

        /// <summary>
        /// Downloads an achievement icon from Steam CDN.
        /// Automatically caches the downloaded icon if caching is enabled.
        /// </summary>
        /// <param name="info">The achievement info</param>
        /// <returns>Downloaded icon bitmap, or null if download failed</returns>
        public async Task<Bitmap?> DownloadIconAsync(AchievementInfo info)
        {
            var icon = info.IsAchieved ? info.IconNormal : info.IconLocked;
            if (string.IsNullOrEmpty(icon))
            {
                return null;
            }

            var fileName = Path.GetFileName(icon);
            if (string.IsNullOrEmpty(fileName) || fileName != icon || !IconNameRegex.IsMatch(fileName))
            {
                return null;
            }

            var builder = new UriBuilder("https", "cdn.steamstatic.com")
            {
                Path = $"/steamcommunity/public/images/apps/{this._gameId}/{Uri.EscapeDataString(fileName)}"
            };

            API.DebugLogger.Log($"Downloading icon from '{builder.Uri}'.");

            var (data, contentType) = await WinForms.ImageDownloader.DownloadImageDataAsync(
                builder.Uri,
                WinForms.HttpClientManager.Client,
                MaxIconBytes);

            if (!WinForms.ImageValidator.IsImageContentType(contentType))
            {
                throw new InvalidDataException("Invalid content type");
            }

            if (!WinForms.ImageValidator.TryValidateAndLoadImage(data, MaxIconBytes, MaxIconDimension, out var image))
            {
                return null;
            }

            Bitmap bitmap = image as Bitmap ?? new Bitmap(image!);

            if (this._useIconCache)
            {
                var cachePath = this.GetCachePath(info);
                if (cachePath != null)
                {
                    API.DebugLogger.Log($"Caching icon '{info.Id}' to '{cachePath}'.");
                    if (!WinForms.ImageDownloader.TrySaveToCache(cachePath, data))
                    {
                        this._useIconCache = false;
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Adds an achievement to the download queue.
        /// </summary>
        /// <param name="info">The achievement info to queue</param>
        public void QueueIcon(AchievementInfo info)
        {
            this._iconQueue.Add(info);
        }

        /// <summary>
        /// Dequeues the next achievement icon for download.
        /// </summary>
        /// <returns>The next achievement in queue, or null if queue is empty</returns>
        public AchievementInfo? DequeueIcon()
        {
            if (this._iconQueue.Count == 0)
            {
                return null;
            }

            var info = this._iconQueue[0];
            this._iconQueue.RemoveAt(0);
            return info;
        }

        /// <summary>
        /// Clears the download queue.
        /// </summary>
        public void ClearQueue()
        {
            this._iconQueue.Clear();
        }
    }
}
