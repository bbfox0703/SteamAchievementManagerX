/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Drawing;
using System.IO;
using SAM.API;
using SAM.API.Constants;
using SAM.API.Utilities;

namespace SAM.WinForms
{
    /// <summary>
    /// Helper class for loading images from local cache with validation.
    /// </summary>
    public static class ImageCacheHelper
    {
        /// <summary>
        /// Attempts to load an image from the cache file with size validation.
        /// </summary>
        /// <param name="cachePath">Full path to the cached image file.</param>
        /// <param name="maxBytes">Maximum allowed file size in bytes.</param>
        /// <returns>A result containing the loaded image or status flags.</returns>
        public static CacheResult TryLoadFromCache(string cachePath, int maxBytes)
        {
            if (!File.Exists(cachePath))
            {
                return new CacheResult(false, null, false);
            }

            try
            {
                var fileInfo = new FileInfo(cachePath);
                if (fileInfo.Length > maxBytes)
                {
                    DebugLogger.Log($"Cache file too large ({fileInfo.Length} > {maxBytes}): {cachePath}");
                    try
                    {
                        File.Delete(cachePath);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"Failed to delete oversized cache file: {ex.Message}");
                    }
                    return new CacheResult(false, null, false);
                }

                using var stream = File.OpenRead(cachePath);
                var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);

                if (image.Width > DownloadLimits.MaxImageDimension || image.Height > DownloadLimits.MaxImageDimension)
                {
                    DebugLogger.Log($"Cache image dimensions too large ({image.Width}x{image.Height}): {cachePath}");
                    image.Dispose();
                    try
                    {
                        File.Delete(cachePath);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"Failed to delete oversized cache image: {ex.Message}");
                    }
                    return new CacheResult(false, null, false);
                }

                return new CacheResult(true, image, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                DebugLogger.Log($"Cache access denied, disabling cache: {ex.Message}");
                return new CacheResult(false, null, true);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Failed to load cache image '{cachePath}': {ex.Message}");
                try
                {
                    File.Delete(cachePath);
                }
                catch (Exception deleteEx)
                {
                    DebugLogger.Log($"Failed to delete corrupted cache file: {deleteEx.Message}");
                }
                return new CacheResult(false, null, false);
            }
        }
    }

    /// <summary>
    /// Result of a cache load operation.
    /// </summary>
    public sealed class CacheResult : IDisposable
    {
        /// <summary>
        /// Whether the image was successfully loaded from cache.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The loaded image, or null if loading failed.
        /// </summary>
        public Image? Image { get; private set; }

        /// <summary>
        /// Whether the cache should be disabled due to permission errors.
        /// </summary>
        public bool DisableCache { get; }

        private bool _disposed;

        public CacheResult(bool success, Image? image, bool disableCache)
        {
            Success = success;
            Image = image;
            DisableCache = disableCache;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Image?.Dispose();
                Image = null;
                _disposed = true;
            }
        }
    }
}
