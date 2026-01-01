#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SAM.WinForms;
using static SAM.Picker.InvariantShorthand;

namespace SAM.Picker.Services
{
    /// <summary>
    /// Manages game logo downloading, caching, and queue processing.
    /// </summary>
    internal class GameLogoDownloader
    {
        private readonly string _iconCacheDirectory;
        private readonly ConcurrentQueue<GameInfo> _logoQueue;
        private readonly HashSet<string> _logosAttempting;
        private readonly HashSet<string> _logosAttempted;
        private readonly object _logoLock;
        private bool _useIconCache;

        private const int MaxLogoBytes = 4 * 1024 * 1024; // 4 MB
        private const int MaxLogoDimension = 1024; // px

        /// <summary>
        /// Gets the number of logos pending in the download queue.
        /// </summary>
        public int QueueCount => this._logoQueue.Count;

        /// <summary>
        /// Initializes a new instance of the GameLogoDownloader.
        /// </summary>
        /// <param name="iconCacheDirectory">Directory path for caching logos</param>
        /// <param name="useIconCache">Whether to use icon caching</param>
        public GameLogoDownloader(string iconCacheDirectory, bool useIconCache)
        {
            this._iconCacheDirectory = iconCacheDirectory;
            this._useIconCache = useIconCache;
            this._logoQueue = new ConcurrentQueue<GameInfo>();
            this._logosAttempting = new HashSet<string>();
            this._logosAttempted = new HashSet<string>();
            this._logoLock = new object();
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
        /// Attempts to load a logo from cache.
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="targetSize">Target image size for resizing</param>
        /// <param name="logo">Output parameter for the loaded and resized logo</param>
        /// <returns>True if logo was loaded from cache, false otherwise</returns>
        public bool TryLoadFromCache(uint gameId, Size targetSize, out Bitmap? logo)
        {
            logo = null;

            if (!this._useIconCache)
            {
                return false;
            }

            string cacheFile = Path.Combine(this._iconCacheDirectory, gameId + ".png");

            try
            {
                if (ImageValidator.TryLoadImageFromCache(cacheFile, MaxLogoBytes, MaxLogoDimension, out var cachedImage))
                {
                    logo = cachedImage.ResizeToFit(targetSize);
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
        /// Downloads a game logo from Steam CDN with fallback URLs.
        /// Automatically caches the downloaded logo if caching is enabled.
        /// </summary>
        /// <param name="info">The game info</param>
        /// <param name="targetSize">Target image size for resizing</param>
        /// <param name="httpClient">HTTP client for downloading</param>
        /// <returns>Downloaded and resized logo bitmap, or null if download failed</returns>
        public async Task<Bitmap?> DownloadLogoAsync(GameInfo info, Size targetSize, System.Net.Http.HttpClient httpClient)
        {
            List<string> urls = new() { info.ImageUrl };
            var fallbackUrl = _($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{info.Id}/header.jpg");
            if (urls.Contains(fallbackUrl) == false)
            {
                urls.Add(fallbackUrl);
            }

            string? cacheFile = null;
            if (this._useIconCache)
            {
                cacheFile = Path.Combine(this._iconCacheDirectory, info.Id + ".png");
            }

            foreach (var url in urls)
            {
                lock (this._logoLock)
                {
                    this._logosAttempted.Add(url);
                }

                if (ImageUrlValidator.TryCreateUri(url, out var uri) == false || uri == null)
                {
                    continue;
                }

                try
                {
                    var (data, contentType) = await ImageDownloader.DownloadImageDataAsync(
                        uri,
                        httpClient,
                        MaxLogoBytes);

                    if (!ImageValidator.IsImageContentType(contentType))
                    {
                        continue;
                    }

                    if (ImageValidator.TryValidateAndLoadImage(data, MaxLogoBytes, MaxLogoDimension, out var image))
                    {
                        Bitmap bitmap = image.ResizeToFit(targetSize);
                        info.ImageUrl = url;

                        if (this._useIconCache && cacheFile != null)
                        {
                            var cacheData = bitmap.ToPngBytes();
                            if (!ImageDownloader.TrySaveToCache(cacheFile, cacheData))
                            {
                                this._useIconCache = false;
                            }
                        }

                        return bitmap;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Queues a game logo for download if not already attempted.
        /// </summary>
        /// <param name="info">The game info</param>
        /// <param name="imageUrl">The image URL to download</param>
        public void QueueLogo(GameInfo info, string imageUrl)
        {
            lock (this._logoLock)
            {
                if (!this._logosAttempting.Contains(imageUrl) && !this._logosAttempted.Contains(imageUrl))
                {
                    this._logosAttempting.Add(imageUrl);
                    this._logoQueue.Enqueue(info);
                }
            }
        }

        /// <summary>
        /// Dequeues the next game logo for download.
        /// </summary>
        /// <returns>The next game in queue, or null if queue is empty</returns>
        public GameInfo? DequeueLogo()
        {
            if (this._logoQueue.TryDequeue(out var info))
            {
                return info;
            }
            return null;
        }

        /// <summary>
        /// Clears the download queue and resets attempted tracking.
        /// </summary>
        public void ClearQueue()
        {
            lock (this._logoLock)
            {
                while (this._logoQueue.TryDequeue(out var logo))
                {
                    this._logosAttempted.Remove(logo.ImageUrl);
                }
            }
        }
    }
}
