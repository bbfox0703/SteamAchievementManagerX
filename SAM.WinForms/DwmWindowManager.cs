using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.WinForms
{
    /// <summary>
    /// Manages Desktop Window Manager (DWM) integration for Windows 11 effects.
    /// Provides Mica backdrop and rounded corner functionality.
    /// </summary>
    public static class DwmWindowManager
    {
        // DWM Window Attributes
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // DWM System Backdrop Types
        private const int DWMSBT_MAINWINDOW = 2; // Mica effect

        // DWM Window Corner Preferences
        private const int DWMWCP_ROUND = 2; // Rounded corners

        /// <summary>
        /// Applies Windows 11 Mica backdrop effect to the specified window.
        /// This method only works on Windows 11 (build 22000 or later).
        /// </summary>
        /// <param name="handle">The window handle to apply the effect to.</param>
        /// <param name="isDarkMode">True to use dark mode; false for light mode.</param>
        public static void ApplyMicaEffect(IntPtr handle, bool isDarkMode)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                return; // Windows 11 or later required
            }

            // Apply Mica backdrop
            int backdrop = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, Marshal.SizeOf<int>());

            // Set dark/light mode
            int useDarkMode = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, Marshal.SizeOf<int>());
        }

        /// <summary>
        /// Applies rounded corners to the specified window.
        /// On Windows 11+, uses native DWM rounded corners. On older Windows, uses GDI region fallback.
        /// </summary>
        /// <param name="form">The form to apply rounded corners to.</param>
        public static void ApplyRoundedCorners(Form form)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                // Windows 11: Use native DWM rounded corners
                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, Marshal.SizeOf<int>());
            }
            else
            {
                // Windows 10 or earlier: Use GDI region fallback
                IntPtr region = CreateRoundRectRgn(0, 0, form.Width, form.Height, 8, 8);
                form.Region = Region.FromHrgn(region);
                DeleteObject(region);
            }
        }

        #region P/Invoke Declarations

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }
}
