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
        private readonly Services.GameLogoDownloader _logoDownloader;
        private readonly Presenters.GameListViewAdapter _gameListAdapter;

        private readonly API.Callbacks.AppDataChanged _AppDataChangedCallback;

        private Color _BorderColor;

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

            string iconCacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appcache");
            bool useIconCache = true;
            try
            {
                Directory.CreateDirectory(iconCacheDirectory);
            }
            catch (Exception ex)
            {
                useIconCache = false;
            }

            this._logoDownloader = new Services.GameLogoDownloader(iconCacheDirectory, useIconCache);
            this._gameListAdapter = new Presenters.GameListViewAdapter(this._FilteredGames, this._FilteredGamesLock);

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
            WinForms.DwmWindowManager.ApplyMicaEffect(this.Handle, !WinForms.WindowsThemeDetector.IsLightTheme());
            WinForms.DwmWindowManager.ApplyRoundedCorners(this);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            WinForms.DwmWindowManager.ApplyRoundedCorners(this);
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    this.UpdateColors();
                    WinForms.DwmWindowManager.ApplyMicaEffect(this.Handle, !WinForms.WindowsThemeDetector.IsLightTheme());
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
                WinForms.DwmWindowManager.ApplyMicaEffect(this.Handle, !WinForms.WindowsThemeDetector.IsLightTheme());
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

        private void UpdateColors()
        {
            bool light = WinForms.WindowsThemeDetector.IsLightTheme();
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

            // Filter games using the service
            var filteredList = Services.GameListFilter.FilterGames(
                gamesSnapshot,
                nameSearch,
                wantNormals,
                wantDemos,
                wantMods,
                wantJunk);

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

                // Queue all games for logo download
                foreach (var game in filteredList)
                {
                    this.AddGameToLogoQueue(game);
                }
            }
            finally
            {
                this._GameListView.EndUpdate();
            }
        }

        private void OnGameListViewRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            this._gameListAdapter.OnRetrieveVirtualItem(sender, e);
        }

        private void OnGameListViewSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            this._gameListAdapter.OnSearchForVirtualItem(sender, e);
        }

        private void DoDownloadLogo(object sender, DoWorkEventArgs e)
        {
            var info = e.Argument as GameInfo;
            if (info == null)
            {
                e.Result = null;
                return;
            }

            // Try loading from cache first
            if (this._logoDownloader.TryLoadFromCache(info.Id, this._LogoImageList.ImageSize, out var cachedLogo))
            {
                e.Result = new LogoInfo(info.Id, cachedLogo);
                return;
            }

            // Download logo using the service
            Bitmap? logo = System.Threading.Tasks.Task.Run(async () =>
                await this._logoDownloader.DownloadLogoAsync(info, this._LogoImageList.ImageSize, WinForms.HttpClientManager.Client)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();

            e.Result = new LogoInfo(info.Id, logo);
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

        private void OnDownloadLogo(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && e.Cancelled == false &&
                e.Result is LogoInfo logoInfo &&
                logoInfo.Bitmap != null &&
                this._Games.TryGetValue(logoInfo.Id, out var gameInfo) == true)
            {
                this._GameListView.BeginUpdate();
                var imageIndex = this._LogoImageList.Images.Count;
                this._LogoImageList.Images.Add(gameInfo.ImageUrl, logoInfo.Bitmap);
                gameInfo.ImageIndex = imageIndex;
                this._GameListView.EndUpdate();
            }

            // Always continue to next download, even on error
            this.DownloadNextLogo();
        }

        private void DownloadNextLogo()
        {
            if (this._LogoWorker.IsBusy == true)
            {
                return;
            }

            var info = this._logoDownloader.DequeueLogo();
            if (info == null)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            this._DownloadStatusLabel.Text = $"Downloading {1 + this._logoDownloader.QueueCount} game icons...";
            this._DownloadStatusLabel.Visible = true;

            this._LogoWorker.RunWorkerAsync(info);
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
            else
            {
                this._logoDownloader.QueueLogo(info, imageUrl);
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

            if (!Services.GameLauncher.LaunchGame(info.Id))
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

            // Clear the download queue because we will be showing only one app
            this._logoDownloader.ClearQueue();

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