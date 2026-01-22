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

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using SAM.API;
using SAM.API.Constants;
using SAM.API.Utilities;
using SAM.WinForms;
using Microsoft.Win32;
using static SAM.API.Utilities.InvariantShorthand;
using APITypes = SAM.API.Types;

namespace SAM.Picker
{
    internal partial class GamePicker : Form
    {
        private readonly API.Client _SteamClient;

        private readonly Dictionary<uint, GameInfo> _Games;
        private readonly List<GameInfo> _FilteredGames;

        private readonly object _GamesLock;
        private readonly object _FilteredGamesLock;
        private readonly object _LogoLock;
        private readonly HashSet<string> _LogosAttempting;
        private readonly HashSet<string> _LogosAttempted;
        private readonly ConcurrentQueue<GameInfo> _LogoQueue;

        private readonly string _IconCacheDirectory;
        private bool _UseIconCache;

        private readonly API.Callbacks.AppDataChanged _AppDataChangedCallback;

        private Color _BorderColor;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(IntPtr hmenu, int fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x20000;
                const int WS_MAXIMIZEBOX = 0x10000;
                const int WS_SYSMENU = 0x80000;
                const int WS_THICKFRAME = 0x40000;
                const int CS_DBLCLKS = 0x8;
                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU | WS_THICKFRAME;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }

        public GamePicker(API.Client client)
        {
            this._Games = new();
            this._FilteredGames = new();
            this._GamesLock = new();
            this._FilteredGamesLock = new();
            this._LogoLock = new();
            this._LogosAttempting = new();
            this._LogosAttempted = new();
            this._LogoQueue = new();

            this._IconCacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appcache");
            try
            {
                Directory.CreateDirectory(this._IconCacheDirectory);
                this._UseIconCache = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning($"Failed to create icon cache directory '{this._IconCacheDirectory}': {ex.Message}");
                this._UseIconCache = false;
            }

            this.InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this._PickerToolStrip.MouseDown += this.OnDragWindow;
            this._PickerStatusStrip.MouseDown += this.OnDragWindow;

            Bitmap blank = new(this._LogoImageList.ImageSize.Width, this._LogoImageList.ImageSize.Height);
            using (var g = Graphics.FromImage(blank))
            {
                g.Clear(Color.DimGray);
            }

            this._LogoImageList.Images.Add("Blank", blank);

            this._SteamClient = client;
            this.FormClosed += (_, _) =>
            {
                SystemEvents.UserPreferenceChanged -= this.OnUserPreferenceChanged;
            };

            SystemEvents.UserPreferenceChanged += this.OnUserPreferenceChanged;

            this._AppDataChangedCallback = client.CreateAndRegisterCallback<API.Callbacks.AppDataChanged>();
            this._AppDataChangedCallback.OnRun += this.OnAppDataChanged;

            this.AddGames();

            this.UpdateColors();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            this.TryApplyMica();
            this.ApplyRoundedCorners();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.ApplyRoundedCorners();
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    this.UpdateColors();
                    this.TryApplyMica();
                }));
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WindowMessages.WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HitTestResults.HTCLIENT)
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                    Point pt = this.PointToClient(new Point(x, y));
                    const int grip = 8;
                    bool top = pt.Y < grip;
                    bool left = pt.X < grip;
                    bool right = pt.X >= this.ClientSize.Width - grip;
                    bool bottom = pt.Y >= this.ClientSize.Height - grip;

                    if (top && left) m.Result = (IntPtr)HitTestResults.HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)HitTestResults.HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)HitTestResults.HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)HitTestResults.HTBOTTOMRIGHT;
                    else if (top) m.Result = (IntPtr)HitTestResults.HTTOP;
                    else if (left) m.Result = (IntPtr)HitTestResults.HTLEFT;
                    else if (right) m.Result = (IntPtr)HitTestResults.HTRIGHT;
                    else if (bottom) m.Result = (IntPtr)HitTestResults.HTBOTTOM;
                    else
                    {
                        Control? child = this.GetChildAtPoint(pt);
                        if (child == null)
                        {
                            m.Result = (IntPtr)HitTestResults.HTCAPTION;
                        }
                    }
                }
                return;
            }
            else if (m.Msg == WindowMessages.WM_NCRBUTTONUP && m.WParam == (IntPtr)HitTestResults.HTCAPTION)
            {
                int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                this.ShowSystemMenu(new Point(x, y));
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WindowMessages.WM_PAINT)
            {
                using var g = Graphics.FromHwnd(this.Handle);
                using var pen = new Pen(this._BorderColor);
                g.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
            else if (m.Msg == WindowMessages.WM_SETTINGCHANGE || m.Msg == WindowMessages.WM_THEMECHANGED)
            {
                this.UpdateColors();
                this.TryApplyMica();
            }
        }

        private void OnDragWindow(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (ReferenceEquals(sender, this._PickerToolStrip) && this._PickerToolStrip.GetItemAt(e.Location) != null)
                {
                    return;
                }
                ReleaseCapture();
                SendMessage(this.Handle, WindowMessages.WM_NCLBUTTONDOWN, (IntPtr)HitTestResults.HTCAPTION, IntPtr.Zero);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Point screen = ((Control)sender!).PointToScreen(e.Location);
                this.ShowSystemMenu(screen);
            }
        }

        private void ShowSystemMenu(Point screenPoint)
        {
            IntPtr hMenu = GetSystemMenu(this.Handle, false);
            int command = TrackPopupMenuEx(hMenu, TrackPopupMenuFlags.TPM_RIGHTBUTTON | TrackPopupMenuFlags.TPM_RETURNCMD, screenPoint.X, screenPoint.Y, this.Handle, IntPtr.Zero);
            if (command != 0)
            {
                SendMessage(this.Handle, WindowMessages.WM_SYSCOMMAND, (IntPtr)command, IntPtr.Zero);
            }
        }

        private void OnCloseButtonClick(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void TryApplyMica()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) == false)
            {
                return;
            }

            int backdrop = DwmAttributes.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(this.Handle, DwmAttributes.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, Marshal.SizeOf<int>());

            int dark = this.IsLightTheme() ? 0 : 1;
            DwmSetWindowAttribute(this.Handle, DwmAttributes.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, Marshal.SizeOf<int>());
        }

        private void ApplyRoundedCorners()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                int pref = DwmAttributes.DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DwmAttributes.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, Marshal.SizeOf<int>());
            }
            else
            {
                IntPtr rgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, 8, 8);
                this.Region = Region.FromHrgn(rgn);
                DeleteObject(rgn);
            }
        }

        private bool IsLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int i)
                {
                    return i != 0;
                }
            }
            catch
            {
            }
            return true;
        }

        private void UpdateColors()
        {
            bool light = this.IsLightTheme();
            if (light)
            {
                this._BorderColor = Color.FromArgb(200, 200, 200);
                this.BackColor = Color.White;
                this.ForeColor = Color.Black;
            }
            else
            {
                this._BorderColor = Color.FromArgb(50, 50, 50);
                this.BackColor = Color.FromArgb(32, 32, 32);
                this.ForeColor = Color.White;
            }

            ThemeHelper.ApplyTheme(this, this.BackColor, this.ForeColor);

            this.Invalidate();
        }

        private void OnAppDataChanged(APITypes.AppDataChanged param)
        {
            if (param.Result == false)
            {
                return;
            }

            if (this._Games.TryGetValue(param.Id, out var game) == false)
            {
                return;
            }

            game.Name = this._SteamClient.SteamApps001.GetAppData(game.Id, "name");

            this.AddGameToLogoQueue(game);
            this.DownloadNextLogo();
        }

        private async System.Threading.Tasks.Task<(byte[] Data, string ContentType)> DownloadDataAsync(Uri uri)
        {
            HttpResponseMessage response = await WinForms.HttpClientManager.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound &&
                uri.Host.Equals("shared.cloudflare.steamstatic.com", StringComparison.OrdinalIgnoreCase))
            {
                response.Dispose();
                var fallbackUri = new UriBuilder(uri) { Host = "shared.steamstatic.com" }.Uri;
                response = await WinForms.HttpClientManager.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, fallbackUri), HttpCompletionOption.ResponseHeadersRead);
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == null)
                {
                    DebugLogger.Log(_($"Missing Content-Length header for {response.RequestMessage!.RequestUri}"));
                }
                else if (contentLength.Value > DownloadLimits.MaxGameLogoBytes)
                {
                    throw new HttpRequestException("Response too large");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

                using var stream = await response.Content.ReadAsStreamAsync();
                var data = StreamHelper.ReadWithLimit(stream, DownloadLimits.MaxGameLogoBytes);
                return (data, contentType);
            }
        }

        private void DoDownloadList(object sender, DoWorkEventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(() =>
            {
                this._PickerStatusLabel.Text = "Downloading game list...";
            }));

            bool usedLocal;
            byte[] bytes = GameList.Load(
                AppDomain.CurrentDomain.BaseDirectory,
                WinForms.HttpClientManager.Client,
                out usedLocal);

            //Silent load from local file if network fails
            //if (usedLocal == true)
            //{
            //    e.Result = "Loaded bundled game list due to network failure.";
            //}

            List<KeyValuePair<uint, string>> pairs = new();
            using (MemoryStream stream = new(bytes, false))
            {
                XmlReaderSettings settings = new()
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                };
                using var reader = XmlReader.Create(stream, settings);
                var document = new XPathDocument(reader);
                var navigator = document.CreateNavigator();
                var nodes = navigator.Select("/games/game");
                while (nodes.MoveNext() == true)
                {
                    string type = nodes.Current?.GetAttribute("type", "") ?? string.Empty;
                    if (nodes.Current == null)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(type) == true)
                    {
                        type = "normal";
                    }
                    pairs.Add(new((uint)nodes.Current.ValueAsLong, type));
                }
            }

            this.BeginInvoke(new MethodInvoker(() =>
            {
                this._PickerStatusLabel.Text = "Checking game ownership...";
            }));
            foreach (var kv in pairs)
            {
                if (this._Games.ContainsKey(kv.Key) == true)
                {
                    continue;
                }
                this.AddGame(kv.Key, kv.Value);
            }
        }

        private void OnDownloadList(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled == true)
            {
                this.AddDefaultGames();
                MessageBox.Show(
                    "Unable to load game list from network or local file. Using default list.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else if (e.Result is string message)
            {
                MessageBox.Show(
                    message,
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            this.RefreshGames();
            this.SaveOwnedGames();
            this._RefreshGamesButton.Enabled = true;
            this.DownloadNextLogo();
        }

        private void RefreshGames()
        {
            var nameSearch = this._SearchGameTextBox.Text.Length > 0
                ? this._SearchGameTextBox.Text
                : null;

            var wantNormals = this._FilterGamesMenuItem.Checked == true;
            var wantDemos = this._FilterDemosMenuItem.Checked == true;
            var wantMods = this._FilterModsMenuItem.Checked == true;
            var wantJunk = this._FilterJunkMenuItem.Checked == true;

            // Create a snapshot of games while holding the lock
            List<GameInfo> gamesSnapshot;
            int totalGamesCount;
            lock (this._GamesLock)
            {
                gamesSnapshot = this._Games.Values.OrderBy(gi => gi.Name).ToList();
                totalGamesCount = this._Games.Count;
            }

            // Build filtered list from snapshot
            var filteredList = new List<GameInfo>();
            foreach (var info in gamesSnapshot)
            {
                if (nameSearch != null &&
                    info.Name.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool wanted = info.Type switch
                {
                    "normal" => wantNormals,
                    "demo" => wantDemos,
                    "mod" => wantMods,
                    "junk" => wantJunk,
                    _ => true,
                };
                if (wanted == false)
                {
                    continue;
                }

                filteredList.Add(info);
            }

            // Update UI while preventing ListView paint events
            this._GameListView.BeginUpdate();
            try
            {
                lock (this._FilteredGamesLock)
                {
                    this._FilteredGames.Clear();
                    this._FilteredGames.AddRange(filteredList);
                    this._GameListView.VirtualListSize = this._FilteredGames.Count;
                }

                this._PickerStatusLabel.Text =
                    $"Displaying {this._GameListView.Items.Count} games. Total {totalGamesCount} games.";

                if (this._GameListView.Items.Count > 0)
                {
                    this._GameListView.Items[0].Selected = true;
                    this._GameListView.Select();
                }
            }
            finally
            {
                this._GameListView.EndUpdate();
            }
        }

        private void OnGameListViewRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            lock (this._FilteredGamesLock)
            {
                // Bounds check to prevent race condition
                if (e.ItemIndex < 0 || e.ItemIndex >= this._FilteredGames.Count)
                {
                    e.Item = new ListViewItem("Loading...");
                    return;
                }

                var info = this._FilteredGames[e.ItemIndex];
                e.Item = info.Item = new()
                {
                    Text = info.Name,
                    ImageIndex = info.ImageIndex,
                };
            }
        }

        private void OnGameListViewSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (e.Direction != SearchDirectionHint.Down || e.IsTextSearch == false)
            {
                return;
            }

            lock (this._FilteredGamesLock)
            {
                var count = this._FilteredGames.Count;
                if (count < 2)
                {
                    return;
                }

                var text = e.Text ?? string.Empty;
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                int startIndex = e.StartIndex;

                Predicate<GameInfo> predicate;
                /*if (e.IsPrefixSearch == true)*/
                {
                    predicate = gi => gi.Name != null && gi.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
                }
                /*else
                {
                    predicate = gi => gi.Name != null && string.Compare(gi.Name, text, StringComparison.CurrentCultureIgnoreCase) == 0;
                }*/

                int index;
                if (e.StartIndex >= count)
                {
                    // starting from the last item in the list
                    index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
                }
                else if (startIndex <= 0)
                {
                    // starting from the first item in the list
                    index = this._FilteredGames.FindIndex(0, count, predicate);
                }
                else
                {
                    index = this._FilteredGames.FindIndex(startIndex, count - startIndex, predicate);
                    if (index < 0)
                    {
                        index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
                    }
                }

                e.Index = index < 0 ? -1 : index;
            }
        }

        private void DoDownloadLogo(object sender, DoWorkEventArgs e)
        {
            var info = e.Argument as GameInfo;
            if (info == null)
            {
                e.Result = null;
                return;
            }

            List<string> urls = new() { info.ImageUrl };
            var fallbackUrl = _($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{info.Id}/header.jpg");
            if (urls.Contains(fallbackUrl) == false)
            {
                urls.Add(fallbackUrl);
            }

            string? cacheFile = null;
            if (this._UseIconCache == true)
            {
                cacheFile = Path.Combine(this._IconCacheDirectory, info.Id + ".png");
                using var cacheResult = ImageCacheHelper.TryLoadFromCache(cacheFile, DownloadLimits.MaxGameLogoBytes);
                if (cacheResult.DisableCache)
                {
                    this._UseIconCache = false;
                }
                else if (cacheResult.Success && cacheResult.Image != null)
                {
                    Bitmap bitmap = cacheResult.Image.ResizeToFit(this._LogoImageList.ImageSize);
                    e.Result = new LogoInfo(info.Id, bitmap);
                    return;
                }
            }

            foreach (var url in urls)
            {
                this._LogosAttempted.Add(url);

                if (ImageUrlValidator.TryCreateUri(url, out var uri) == false)
                {
                    DebugLogger.Log(_($"Invalid image URL for app {info.Id}: {url}"));
                    continue;
                }

                if (uri == null)
                {
                    continue;
                }

                Uri nonNullUri = uri;

                var result = TryDownloadAndProcessImage(nonNullUri, info.Id, this._LogoImageList.ImageSize);
                if (result.success)
                {
                    e.Result = new LogoInfo(info.Id, result.bitmap);
                    info.ImageUrl = url;
                    SaveToCache(result.bitmap, cacheFile);
                    return;
                }
                if (result.fatalError)
                {
                    e.Result = new LogoInfo(info.Id, null);
                    return;
                }
            }

            e.Result = new LogoInfo(info.Id, null);
        }

        /// <summary>
        /// Downloads and processes an image from the given URI.
        /// </summary>
        private (bool success, bool fatalError, Bitmap? bitmap) TryDownloadAndProcessImage(Uri uri, uint appId, Size targetSize)
        {
            try
            {
                var (data, contentType) = System.Threading.Tasks.Task.Run(async () =>
                    await this.DownloadDataAsync(uri).ConfigureAwait(false)
                ).GetAwaiter().GetResult();

                if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == false)
                {
                    DebugLogger.Log(_($"Invalid content type for app {appId}: {contentType}"));
                    return (false, false, null);
                }

                return TryProcessImageData(data, appId, targetSize);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(_($"Failed to download image for app {appId} from {uri}: {ex}"));
                return (false, false, null);
            }
        }

        /// <summary>
        /// Validates image data and resizes it to the target size.
        /// </summary>
        private (bool success, bool fatalError, Bitmap? bitmap) TryProcessImageData(byte[] data, uint appId, Size targetSize)
        {
            using var stream = new MemoryStream(data, false);
            try
            {
                using var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);

                if (image.Width > DownloadLimits.MaxImageDimension || image.Height > DownloadLimits.MaxImageDimension)
                {
                    DebugLogger.Log(_($"Image dimensions too large for app {appId}: {image.Width}x{image.Height}"));
                    return (false, false, null);
                }

                var bitmap = image.ResizeToFit(targetSize);
                return (true, false, bitmap);
            }
            catch (ArgumentException)
            {
                return (false, true, null);
            }
            catch (OutOfMemoryException)
            {
                return (false, true, null);
            }
        }

        /// <summary>
        /// Saves a bitmap to the local cache as PNG.
        /// </summary>
        private void SaveToCache(Bitmap? bitmap, string? cacheFile)
        {
            if (bitmap == null || cacheFile == null || this._UseIconCache == false)
            {
                return;
            }

            try
            {
                var cacheData = bitmap.ToPngBytes();
                File.WriteAllBytes(cacheFile, cacheData);
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning($"Failed to save logo to cache '{cacheFile}': {ex.Message}");
                this._UseIconCache = false;
            }
        }

        private void OnDownloadLogo(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled == true)
            {
                return;
            }

            if (e.Result is LogoInfo logoInfo &&
                logoInfo.Bitmap != null &&
                this._Games.TryGetValue(logoInfo.Id, out var gameInfo) == true)
            {
                this._GameListView.BeginUpdate();
                var imageIndex = this._LogoImageList.Images.Count;
                this._LogoImageList.Images.Add(gameInfo.ImageUrl, logoInfo.Bitmap);
                gameInfo.ImageIndex = imageIndex;
                this._GameListView.EndUpdate();
            }

            this.DownloadNextLogo();
        }

        private void DownloadNextLogo()
        {
            lock (this._LogoLock)
            {

                if (this._LogoWorker.IsBusy == true)
                {
                    return;
                }

                GameInfo? info;
                while (true)
                {
                    if (this._LogoQueue.TryDequeue(out info) == false)
                    {
                        this._DownloadStatusLabel.Visible = false;
                        return;
                    }

                    if (info == null)
                    {
                        continue;
                    }

                    if (info.Item == null)
                    {
                        continue;
                    }

                    if (this._FilteredGames.Contains(info) == false ||
                        info.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
                    {
                        this._LogosAttempting.Remove(info.ImageUrl);
                        continue;
                    }

                    break;
                }

                this._DownloadStatusLabel.Text = $"Downloading {1 + this._LogoQueue.Count} game icons...";
                this._DownloadStatusLabel.Visible = true;

                this._LogoWorker.RunWorkerAsync(info!);
            }
        }

        private string GetGameImageUrl(uint id)
        {
            var currentLanguage = LanguageHelper.GetCurrentLanguage(_LanguageComboBox, this._SteamClient.SteamApps008);
            var url = GameImageUrlResolver.GetGameImageUrl(this._SteamClient.SteamApps001.GetAppData, id, currentLanguage);
            return url ?? _($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{id}/header.jpg");
        }

        private void AddGameToLogoQueue(GameInfo info)
        {
            if (info.ImageIndex > 0)
            {
                return;
            }

            var imageUrl = GetGameImageUrl(info.Id);
            if (string.IsNullOrEmpty(imageUrl) == true)
            {
                return;
            }

            info.ImageUrl = imageUrl;

            int imageIndex = this._LogoImageList.Images.IndexOfKey(imageUrl);
            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else if (
                this._LogosAttempting.Contains(imageUrl) == false &&
                this._LogosAttempted.Contains(imageUrl) == false)
            {
                this._LogosAttempting.Add(imageUrl);
                this._LogoQueue.Enqueue(info);
            }
        }

        private bool OwnsGame(uint id)
        {
            return this._SteamClient.SteamApps008.IsSubscribedApp(id);
        }

        private void AddGame(uint id, string type)
        {
            lock (this._GamesLock)
            {
                if (this._Games.ContainsKey(id) == true)
                {
                    return;
                }

                if (this.OwnsGame(id) == false)
                {
                    return;
                }

                GameInfo info = new(id, type);
                info.Name = this._SteamClient.SteamApps001.GetAppData(info.Id, "name");
                this._Games.Add(id, info);
            }
        }

        private void LoadCachedOwnedGames()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usergames.xml");
                if (File.Exists(path) == false)
                {
                    return;
                }

                using var stream = File.OpenRead(path);
                XmlReaderSettings settings = new()
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                };
                using var reader = XmlReader.Create(stream, settings);
                var document = new XPathDocument(reader);
                var navigator = document.CreateNavigator();
                var nodes = navigator.Select("/games/game");
                while (nodes.MoveNext() == true)
                {
                    if (nodes.Current == null)
                    {
                        continue;
                    }

                    string idText = nodes.Current.GetAttribute("id", "");
                    if (uint.TryParse(idText, out var id) == false)
                    {
                        continue;
                    }
                    if (this._Games.ContainsKey(id) == true)
                    {
                        continue;
                    }
                    if (this.OwnsGame(id) == false)
                    {
                        continue;
                    }
                    GameInfo info = new(id, null);
                    info.Name = this._SteamClient.SteamApps001.GetAppData(info.Id, "name");
                    this._Games.Add(id, info);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(ex);
            }
        }

        private void AddGames()
        {
            lock (this._GamesLock)
            {
                this._Games.Clear();
            }
            this.LoadCachedOwnedGames();
            this.RefreshGames();
            this._RefreshGamesButton.Enabled = false;
            this._ListWorker.RunWorkerAsync();
        }

        private void AddDefaultGames()
        {
            this.AddGame(480, "normal"); // Spacewar
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void SaveOwnedGames()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usergames.xml");
                string tempPath = path + ".tmp";

                using (var writer = XmlWriter.Create(tempPath, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
                {
                    writer.WriteStartElement("games");
                    foreach (var id in this._Games.Keys.OrderBy(k => k))
                    {
                        writer.WriteStartElement("game");
                        writer.WriteAttributeString("id", id.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                if (File.Exists(path) == true)
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(ex);
            }
        }

        private void OnActivateGame(object sender, EventArgs e)
        {
            var focusedItem = (sender as MyListView)?.FocusedItem;
            var index = focusedItem != null ? focusedItem.Index : -1;
            if (index < 0 || index >= this._FilteredGames.Count)
            {
                return;
            }

            var info = this._FilteredGames[index];
            if (info == null)
            {
                return;
            }

            string gameExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SAM.Game.exe");
            if (File.Exists(gameExe) == false)
            {
                MessageBox.Show(
                    this,
                    "SAM.Game.exe is missing.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process.Start(gameExe, info.Id.ToString(CultureInfo.InvariantCulture));
            }
            catch (Win32Exception)
            {
                MessageBox.Show(
                    this,
                    "Failed to start SAM.Game.exe.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this._AddGameTextBox.Text = "";
            this.AddGames();
        }

        private void OnAddGame(object sender, EventArgs e)
        {
            uint id;

            if (uint.TryParse(this._AddGameTextBox.Text, out id) == false)
            {
                MessageBox.Show(
                    this,
                    "Please enter a valid game ID.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (this.OwnsGame(id) == false)
            {
                MessageBox.Show(this, "You don't own that game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (this._LogoQueue.TryDequeue(out var logo) == true)
            {
                // clear the download queue because we will be showing only one app
                this._LogosAttempted.Remove(logo.ImageUrl);
            }

            this._AddGameTextBox.Text = "";
            lock (this._GamesLock)
            {
                this._Games.Clear();
            }
            this.AddGame(id, "normal");
            this._FilterGamesMenuItem.Checked = true;
            this.RefreshGames();
            this.DownloadNextLogo();
        }

        private void OnFilterUpdate(object sender, EventArgs e)
        {
            this.RefreshGames();

            // Compatibility with _GameListView SearchForVirtualItemEventHandler (otherwise _SearchGameTextBox loose focus on KeyUp)
            this._SearchGameTextBox.Focus();
        }

        private void OnGameListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;

            if (e.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
            {
                return;
            }

            lock (this._FilteredGamesLock)
            {
                // Bounds check to prevent race condition
                if (e.ItemIndex < 0 || e.ItemIndex >= this._FilteredGames.Count)
                {
                    return;
                }

                var info = this._FilteredGames[e.ItemIndex];
                if (info.ImageIndex <= 0)
                {
                    this.AddGameToLogoQueue(info);
                    this.DownloadNextLogo();
                }
            }
        }
    }
}