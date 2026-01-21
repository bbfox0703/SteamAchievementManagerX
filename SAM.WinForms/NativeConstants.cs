#nullable enable

namespace SAM.WinForms
{
    /// <summary>
    /// Windows message constants for WndProc handling.
    /// </summary>
    public static class WindowMessages
    {
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_PAINT = 0x000F;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_THEMECHANGED = 0x031A;
        public const int WM_NCRBUTTONUP = 0x00A5;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_SYSCOMMAND = 0x0112;
    }

    /// <summary>
    /// Hit test result constants for WM_NCHITTEST.
    /// </summary>
    public static class HitTestResults
    {
        public const int HTCLIENT = 1;
        public const int HTCAPTION = 2;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;
    }

    /// <summary>
    /// Track popup menu flags.
    /// </summary>
    public static class TrackPopupMenuFlags
    {
        public const int TPM_LEFTBUTTON = 0x0000;
        public const int TPM_RIGHTBUTTON = 0x0002;
        public const int TPM_RETURNCMD = 0x0100;
    }

    /// <summary>
    /// Desktop Window Manager (DWM) attribute constants.
    /// </summary>
    public static class DwmAttributes
    {
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public const int DWMSBT_MAINWINDOW = 2;
        public const int DWMWCP_ROUND = 2;
    }
}
