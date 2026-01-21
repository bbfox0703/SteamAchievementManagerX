#nullable enable

namespace SAM.API.Constants
{
    /// <summary>
    /// Defines size limits for various download operations.
    /// </summary>
    public static class DownloadLimits
    {
        /// <summary>
        /// Maximum allowed size for game list XML downloads (4 MB).
        /// </summary>
        public const int MaxGameListBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Maximum allowed size for game logo image downloads (4 MB).
        /// </summary>
        public const int MaxGameLogoBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Maximum allowed size for achievement icon downloads (512 KB).
        /// </summary>
        public const int MaxAchievementIconBytes = 512 * 1024;

        /// <summary>
        /// Maximum allowed dimension for images in pixels.
        /// </summary>
        public const int MaxImageDimension = 1024;
    }
}
