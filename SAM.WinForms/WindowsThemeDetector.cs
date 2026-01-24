using Microsoft.Win32;

namespace SAM.WinForms
{
    /// <summary>
    /// Detects the current Windows theme (light or dark mode).
    /// </summary>
    public static class WindowsThemeDetector
    {
        /// <summary>
        /// Determines whether the current Windows theme is light mode.
        /// </summary>
        /// <returns>
        /// <c>true</c> if Windows is using light theme; <c>false</c> if using dark theme.
        /// Returns <c>true</c> (light theme) by default if the registry key is not found or cannot be read.
        /// </returns>
        public static bool IsLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int themeValue)
                {
                    return themeValue != 0;
                }
            }
            catch
            {
                // Registry access failed or key doesn't exist
                // Fall back to light theme as default
            }

            return true; // Default to light theme
        }
    }
}
