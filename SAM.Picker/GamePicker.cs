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
using SAM.WinForms;
using Microsoft.Win32;
using static SAM.Picker.InvariantShorthand;
using APITypes = SAM.API.Types;

namespace SAM.Picker
{
    internal partial class GamePicker : Form
    {
        private readonly API.Client _SteamClient;
        private readonly HttpClient _HttpClient;

        private readonly Dictionary<uint, GameInfo> _Games;
        private readonly List<GameInfo> _FilteredGames;

        private readonly object _LogoLock;
        private readonly HashSet<string> _LogosAttempting;
        private readonly HashSet<string> _LogosAttempted;
        private readonly ConcurrentQueue<GameInfo> _LogoQueue;

        private readonly string _IconCacheDirectory;
        private bool _UseIconCache;

        private readonly API.Callbacks.AppDataChanged _AppDataChangedCallback;

        private Color _BorderColor;

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_PAINT = 0x000F;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_THEMECHANGED = 0x031A;
        private const int WM_NCRBUTTONUP = 0x00A5;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int TPM_LEFTBUTTON = 0x0000;
        private const int TPM_RIGHTBUTTON = 0x0002;
        private const int TPM_RETURNCMD = 0x0100;

        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMSBT_MAINWINDOW = 2;
        private const int DWMWCP_ROUND = 2;

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
            catch (Exception)
            {
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
            this._HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            this.FormClosed += (_, _) =>
            {
                this._HttpClient.Dispose();
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
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                    Point pt = this.PointToClient(new Point(x, y));
                    const int grip = 8;
                    bool top = pt.Y < grip;
                    bool left = pt.X < grip;
                    bool right = pt.X >= this.ClientSize.Width - grip;
                    bool bottom = pt.Y >= this.ClientSize.Height - grip;

                    if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                    else
                    {
                        Control? child = this.GetChildAtPoint(pt);
                        if (child == null)
                        {
                            m.Result = (IntPtr)HTCAPTION;
                        }
                    }
                }
                return;
            }
            else if (m.Msg == WM_NCRBUTTONUP && m.WParam == (IntPtr)HTCAPTION)
            {
                int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                this.ShowSystemMenu(new Point(x, y));
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                using var g = Graphics.FromHwnd(this.Handle);
                using var pen = new Pen(this._BorderColor);
                g.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
            else if (m.Msg == WM_SETTINGCHANGE || m.Msg == WM_THEMECHANGED)
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
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
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
            int command = TrackPopupMenuEx(hMenu, TPM_RIGHTBUTTON | TPM_RETURNCMD, screenPoint.X, screenPoint.Y, this.Handle, IntPtr.Zero);
            if (command != 0)
            {
                SendMessage(this.Handle, WM_SYSCOMMAND, (IntPtr)command, IntPtr.Zero);
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

            int backdrop = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(this.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, Marshal.SizeOf<int>());

            int dark = this.IsLightTheme() ? 0 : 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, Marshal.SizeOf<int>());
        }

        private void ApplyRoundedCorners()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, Marshal.SizeOf<int>());
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

        private const int MaxLogoBytes = 4 * 1024 * 1024; // 4 MB
        private const int MaxLogoDimension = 1024; // px

        private async System.Threading.Tasks.Task<(byte[] Data, string ContentType)> DownloadDataAsync(Uri uri)
        {
            HttpResponseMessage response = await this._HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound &&
                uri.Host.Equals("shared.cloudflare.steamstatic.com", StringComparison.OrdinalIgnoreCase))
            {
                response.Dispose();
                var fallbackUri = new UriBuilder(uri) { Host = "shared.steamstatic.com" }.Uri;
                response = await this._HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, fallbackUri), HttpCompletionOption.ResponseHeadersRead);
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == null)
                {
                    Debug.WriteLine(_($"Missing Content-Length header for {response.RequestMessage!.RequestUri}"));
                }
                else if (contentLength.Value > MaxLogoBytes)
                {
                    throw new HttpRequestException("Response too large");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

                using var stream = await response.Content.ReadAsStreamAsync();
                var data = ReadWithLimit(stream, MaxLogoBytes);
                return (data, contentType);
            }
        }

        private static byte[] ReadWithLimit(Stream stream, int maxBytes)
        {
            using MemoryStream memory = new();
            byte[] buffer = new byte[81920];
            int read;
            int total = 0;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new HttpRequestException("Response exceeded maximum allowed size");
                }
                memory.Write(buffer, 0, read);
            }
            return memory.ToArray();
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
                this._HttpClient,
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

            this._FilteredGames.Clear();
            foreach (var info in this._Games.Values.OrderBy(gi => gi.Name))
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

                this._FilteredGames.Add(info);
            }

            this._GameListView.VirtualListSize = this._FilteredGames.Count;
            this._PickerStatusLabel.Text =
                $"Displaying {this._GameListView.Items.Count} games. Total {this._Games.Count} games.";

            if (this._GameListView.Items.Count > 0)
            {
                this._GameListView.Items[0].Selected = true;
                this._GameListView.Select();
            }
        }

        private void OnGameListViewRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var info = this._FilteredGames[e.ItemIndex];
            e.Item = info.Item = new()
            {
                Text = info.Name,
                ImageIndex = info.ImageIndex,
            };
        }

        private void OnGameListViewSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (e.Direction != SearchDirectionHint.Down || e.IsTextSearch == false)
            {
                return;
            }

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
                try
                {
                    if (File.Exists(cacheFile) == true)
                    {
                        var bytes = File.ReadAllBytes(cacheFile);
                        if (bytes.Length <= MaxLogoBytes)
                        {
                            using var stream = new MemoryStream(bytes, false);
                            try
                            {
                                using var image = Image.FromStream(
                                    stream,
                                    useEmbeddedColorManagement: false,
                                    validateImageData: true);
                                if (image.Width <= MaxLogoDimension && image.Height <= MaxLogoDimension)
                                {
                                    Bitmap bitmap = image.ResizeToFit(this._LogoImageList.ImageSize);
                                    e.Result = new LogoInfo(info.Id, bitmap);
                                    return;
                                }
                            }
                            catch (ArgumentException)
                            {
                                try { File.Delete(cacheFile); } catch { }
                            }
                            catch (OutOfMemoryException)
                            {
                                try { File.Delete(cacheFile); } catch { }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    this._UseIconCache = false;
                }
            }

            foreach (var url in urls)
            {
                this._LogosAttempted.Add(url);

                if (ImageUrlValidator.TryCreateUri(url, out var uri) == false)
                {
                    Debug.WriteLine(_($"Invalid image URL for app {info.Id}: {url}"));
                    continue;
                }

                if (uri == null)
                {
                    continue;
                }

                Uri nonNullUri = uri;

                try
                {
                    var (data, contentType) = this.DownloadDataAsync(nonNullUri).GetAwaiter().GetResult();
                    if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        throw new InvalidDataException("Invalid content type");
                    }

                    using (MemoryStream stream = new(data, false))
                    {
                        try
                        {
                            using (var image = Image.FromStream(
                                       stream,
                                       useEmbeddedColorManagement: false,
                                       validateImageData: true))
                            {
                                if (image.Width > MaxLogoDimension || image.Height > MaxLogoDimension)
                                {
                                    throw new InvalidDataException("Image dimensions too large");
                                }

                                Bitmap bitmap = image.ResizeToFit(this._LogoImageList.ImageSize);
                                e.Result = new LogoInfo(info.Id, bitmap);
                                info.ImageUrl = url;

                                if (this._UseIconCache == true && cacheFile != null)
                                {
                                    try
                                    {
                                        var cacheData = bitmap.ToPngBytes();
                                        File.WriteAllBytes(cacheFile, cacheData);
                                    }
                                    catch (Exception)
                                    {
                                        this._UseIconCache = false;
                                    }
                                }
                                return;
                            }
                        }
                        catch (ArgumentException)
                        {
                            e.Result = new LogoInfo(info.Id, null);
                            return;
                        }
                        catch (OutOfMemoryException)
                        {
                            e.Result = new LogoInfo(info.Id, null);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(_($"Failed to download image for app {info.Id} from {url}: {ex}"));
                }
            }

            e.Result = new LogoInfo(info.Id, null);
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

        private static bool TrySanitizeCandidate(string candidate, out string sanitized)
        {
            sanitized = Path.GetFileName(candidate);

            if (candidate.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                candidate.IndexOf(':') >= 0)
            {
                return false;
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Scheme) == false)
            {
                return false;
            }

            return true;
        }

        private string GetGameImageUrl(uint id)
        {
            string candidate;

            var currentLanguage = "";

            if (_LanguageComboBox.Text.Length == 0)
            {
                currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();
                _LanguageComboBox.Text = currentLanguage;
            }
            else
            {
                currentLanguage = _LanguageComboBox.Text;
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, _($"small_capsule/{currentLanguage}"));

            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return _($"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{safeCandidate}");
                }
                else
                {
                    Debug.WriteLine(_($"Invalid small_capsule path for app {id} language {currentLanguage}: {candidate}"));
                }
            }
            else
            {
                Debug.WriteLine(_($"Missing small_capsule for app {id} language {currentLanguage}"));
            }

            if (currentLanguage != "english")
            {
                candidate = this._SteamClient.SteamApps001.GetAppData(id, "small_capsule/english");
                if (string.IsNullOrEmpty(candidate) == false)
                {
                    if (TrySanitizeCandidate(candidate, out var safeCandidate))
                    {
                        return _($"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{safeCandidate}");
                    }
                    else
                    {
                        Debug.WriteLine(_($"Invalid small_capsule path for app {id} language english: {candidate}"));
                    }
                }
                else
                {
                    Debug.WriteLine(_($"Missing small_capsule for app {id} language english"));
                }
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, "logo");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return _($"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{id}/{safeCandidate}.jpg");
                }
                else
                {
                    Debug.WriteLine(_($"Invalid logo path for app {id}: {candidate}"));
                }
            }
            else
            {
                Debug.WriteLine(_($"Missing logo for app {id}"));
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, "library_600x900");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return _($"https://shared.cloudflare.steamstatic.com/steam/apps/{id}/{safeCandidate}");
                }
                else
                {
                    Debug.WriteLine(_($"Invalid library_600x900 path for app {id}: {candidate}"));
                }
            }
            else
            {
                Debug.WriteLine(_($"Missing library_600x900 for app {id}"));
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, "header_image");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return _($"https://shared.cloudflare.steamstatic.com/steam/apps/{id}/{safeCandidate}");
                }
                else
                {
                    Debug.WriteLine(_($"Invalid header_image path for app {id}: {candidate}"));
                }
            }
            else
            {
                Debug.WriteLine(_($"Missing header_image for app {id}"));
            }
            return _($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{id}/header.jpg");

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
                Debug.WriteLine(ex);
            }
        }

        private void AddGames()
        {
            this._Games.Clear();
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
                Debug.WriteLine(ex);
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
            this._Games.Clear();
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

            var info = this._FilteredGames[e.ItemIndex];
            if (info.ImageIndex <= 0)
            {
                this.AddGameToLogoQueue(info);
                this.DownloadNextLogo();
            }
        }
    }
}