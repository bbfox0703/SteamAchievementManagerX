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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SAM.WinForms;
using SAM.API;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using static SAM.Game.InvariantShorthand;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using APITypes = SAM.API.Types;


namespace SAM.Game
{
    internal partial class Manager : Form
    {
        private readonly long _GameId;
        private readonly API.Client _SteamClient;

        private readonly string _IconCacheDirectory;
        private Services.AchievementIconManager _achievementIconManager = null!;

        private const int MaxTimerTextLength = 6; // Maximum digits for timer input
        private const int MouseMoveDistance = 15; // Pixels to move mouse
        private const int MouseMoveDelayMs = 12; // Milliseconds between mouse movements
        private readonly List<Stats.StatDefinition> _statDefinitions = new();

        private readonly List<Stats.AchievementDefinition> _achievementDefinitions = new();

        private readonly BindingList<Stats.StatInfo> _statistics = new();

        private readonly API.Callbacks.UserStatsReceived _userStatsReceivedCallback;

        //private API.Callback<APITypes.UserStatsStored> UserStatsStoredCallback;
        // *****************************************************************
        private Dictionary<string, int> _achievementCounters = new();

        private bool _moveRight = true;
        private POINT _lastMousePos;

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


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern void SetThreadExecutionState(ExecutionState esFlags);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private bool _isAutoMouseMoveEnabled = false;
        [Flags]
        public enum ExecutionState : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }
        public struct POINT
        {
            public int X;
            public int Y;
        }


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
        // *****************************************************************
        public Manager(long gameId, API.Client client)
        {
            this.InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this._MainToolStrip.MouseDown += this.OnDragWindow;
            this._AchievementsToolStrip.MouseDown += this.OnDragWindow;
            this._MainStatusStrip.MouseDown += this.OnDragWindow;

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;
            //this.statisticsList.Enabled = this.checkBox1.Checked;

            this._AchievementImageList.Images.Add("Blank", new Bitmap(64, 64));

            this._StatisticsDataGridView.AutoGenerateColumns = false;

            this._StatisticsDataGridView.Columns.Add("name", "Name");
            this._StatisticsDataGridView.Columns[0].ReadOnly = true;
            this._StatisticsDataGridView.Columns[0].Width = 200;
            this._StatisticsDataGridView.Columns[0].DataPropertyName = "DisplayName";

            this._StatisticsDataGridView.Columns.Add("value", "Value");
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
            this._StatisticsDataGridView.Columns[1].Width = 90;
            this._StatisticsDataGridView.Columns[1].DataPropertyName = "Value";

            this._StatisticsDataGridView.Columns.Add("extra", "Extra");
            this._StatisticsDataGridView.Columns[2].ReadOnly = true;
            this._StatisticsDataGridView.Columns[2].Width = 200;
            this._StatisticsDataGridView.Columns[2].DataPropertyName = "Extra";

            this._StatisticsDataGridView.DataSource = new BindingSource()
            {
                DataSource = this._statistics,
            };

            this._GameId = gameId;
            this._SteamClient = client;

            this.FormClosed += (_, _) =>
            {
                SystemEvents.UserPreferenceChanged -= this.OnUserPreferenceChanged;
            };
            SystemEvents.UserPreferenceChanged += this.OnUserPreferenceChanged;

            this._IconCacheDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appcache",
                gameId.ToString(CultureInfo.InvariantCulture));

            bool useIconCache = true;
            try
            {
                Directory.CreateDirectory(this._IconCacheDirectory);
            }
            catch (Exception)
            {
                useIconCache = false;
            }

            this._achievementIconManager = new Services.AchievementIconManager(
                gameId,
                this._IconCacheDirectory,
                useIconCache);

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                base.Text += " | " + name;
            }
            else
            {
                base.Text += " | " + this._GameId.ToString(CultureInfo.InvariantCulture);
            }

            this._userStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._userStatsReceivedCallback.OnRun += this.OnUserStatsReceived;

            //this.UserStatsStoredCallback = new API.Callback(1102, new API.Callback.CallbackFunction(this.OnUserStatsStored));

            this.RefreshStats();
            this.UpdateButtonText();

            this.UpdateColors();
        }

        private void AddAchievementIcon(Stats.AchievementInfo info, Image? icon)
        {
            if (icon == null)
            {
                info.ImageIndex = 0;
            }
            else
            {
                var key = info.Id + "_" + (info.IsAchieved == true ? "achieved" : "locked");
                info.ImageIndex = this._AchievementImageList.Images.Count;
                this._AchievementImageList.Images.Add(key, new Bitmap(icon));
                icon.Dispose();
            }
        }

        private async void DownloadNextIcon()
        {
            if (this._achievementIconManager.QueueCount == 0)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            this._DownloadStatusLabel.Text = $"Downloading {this._achievementIconManager.QueueCount} icons...";
            this._DownloadStatusLabel.Visible = true;

            var info = this._achievementIconManager.DequeueIcon();
            if (info == null)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            Bitmap? bitmap = null;
            try
            {
                bitmap = await this._achievementIconManager.DownloadIconAsync(info);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(ex);
            }

            this.AddAchievementIcon(info, bitmap);
            this._AchievementListView.Update();

            this.DownloadNextIcon();
        }

        private static string TranslateError(int id) => id switch
        {
            2 => "generic error -- this usually means you don't own the game",
            _ => _($"{id}"),
        };

        private bool LoadUserGameStatsSchema()
        {
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

            var schemaManager = new Services.SchemaManager(this._GameId, currentLanguage);

            this._achievementDefinitions.Clear();
            this._statDefinitions.Clear();

            if (!schemaManager.LoadSchema(out var achievements, out var stats))
            {
                return false;
            }

            this._achievementDefinitions.AddRange(achievements);
            this._statDefinitions.AddRange(stats);

            return true;
        }

        private void OnUserStatsReceived(APITypes.UserStatsReceived param)
        {
            if (param.Result != 1)
            {
                this._GameStatusLabel.Text = $"Error while retrieving stats: {TranslateError(param.Result)}";
                this.EnableInput();
                return;
            }

            if (this.LoadUserGameStatsSchema() == false)
            {
                this._GameStatusLabel.Text = "Failed to load schema.";
                this.EnableInput();
                return;
            }

            try
            {
                this.GetAchievements();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling achievements retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling achievements retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.GetStatistics();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling stats retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling stats retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Synchronize ListView with _achievementCounters
            foreach (ListViewItem item in _AchievementListView.Items)
            {
                string key = item.SubItems[3].Text; // 3rd column is Key

                if (_achievementCounters.TryGetValue(key, out int counter))
                {
                    item.SubItems[4].Text = counter.ToString(); // Update the Counter column
                }
            }

            this._GameStatusLabel.Text = $"Retrieved {this._AchievementListView.Items.Count} achievements and {this._StatisticsDataGridView.Rows.Count} statistics.";
            this.EnableInput();
        }

        private void RefreshStats()
        {
            this._AchievementListView.Items.Clear();
            this._StatisticsDataGridView.Rows.Clear();

            var steamId = this._SteamClient.SteamUser.GetSteamId();

            // This still triggers the UserStatsReceived callback, in addition to the callresult.
            // No need to implement callresults for the time being.
            var callHandle = this._SteamClient.SteamUserStats.RequestUserStats(steamId);
            if (callHandle == API.CallHandle.Invalid)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = "Retrieving stat information...";
            this.DisableInput();
        }

        private bool _IsUpdatingAchievementList;

        private void GetAchievements()
        {
            var textSearch = this._MatchingStringTextBox.Text.Length > 0
                ? this._MatchingStringTextBox.Text
                : null;

            this._IsUpdatingAchievementList = true;

            this._AchievementListView.Items.Clear();
            this._AchievementListView.BeginUpdate();
            //this.Achievements.Clear();

            bool wantLocked = this._DisplayLockedOnlyButton.Checked == true;
            bool wantUnlocked = this._DisplayUnlockedOnlyButton.Checked == true;
            bool light = WinForms.WindowsThemeDetector.IsLightTheme();

            foreach (var def in this._achievementDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id) == true)
                {
                    continue;
                }

                if (this._SteamClient.SteamUserStats.GetAchievementAndUnlockTime(
                    def.Id,
                    out bool isAchieved,
                    out var unlockTime) == false)
                {
                    continue;
                }

                bool wanted = (wantLocked == false && wantUnlocked == false) || isAchieved switch
                {
                    true => wantUnlocked,
                    false => wantLocked,
                };
                if (wanted == false)
                {
                    continue;
                }

                if (textSearch != null)
                {
                    if (def.Name.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (string.IsNullOrEmpty(def.Description) || def.Description.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        continue;
                    }
                }

                Stats.AchievementInfo info = new()
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    UnlockTime = isAchieved == true && unlockTime > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime
                        : null,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description,
                };

                ListViewItem item = new()
                {
                    Checked = isAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (def.Permission & 3) == 0
                        ? this.BackColor
                        : (light
                            ? ControlPaint.Light(this.BackColor)
                            : ControlPaint.Dark(this.BackColor)),
                    ForeColor = this.ForeColor,
                };

                info.Item = item;

                if (item.Text.StartsWith("#", StringComparison.InvariantCulture) == true)
                {
                    item.Text = info.Id;
                    item.SubItems.Add("");
                }
                else
                {
                    item.SubItems.Add(info.Description);
                }

                item.SubItems.Add(info.UnlockTime.HasValue == true
                    ? info.UnlockTime.Value.ToString()
                    : "");

                //----------------
                item.SubItems.Add(info.Id);
                item.SubItems.Add("-1");
                //----------------

                foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                {
                    subItem.BackColor = item.BackColor;
                    subItem.ForeColor = item.ForeColor;
                }

                info.ImageIndex = 0;

                // Queue the icon for download if it's not already available
                this.QueueAchievementIcon(info, false);
                this._AchievementListView.Items.Add(item);
            }

            // Sort using the current column/order before displaying
            this._AchievementListView.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder);
            this._AchievementListView.Sort();
            this._AchievementListView.EndUpdate();
            this._IsUpdatingAchievementList = false;

            this.DownloadNextIcon();
        }

        private void GetStatistics()
        {
            this._statistics.Clear();
            foreach (var stat in this._statDefinitions)
            {
                if (string.IsNullOrEmpty(stat.Id) == true)
                {
                    continue;
                }

                if (stat is Stats.IntegerStatDefinition intStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(intStat.Id, out int value) == false)
                    {
                        continue;
                    }
                    this._statistics.Add(new Stats.IntStatInfo()
                    {
                        Id = intStat.Id,
                        DisplayName = intStat.DisplayName,
                        IntValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = intStat.IncrementOnly,
                        Permission = intStat.Permission,
                    });
                }
                else if (stat is Stats.FloatStatDefinition floatStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(floatStat.Id, out float value) == false)
                    {
                        continue;
                    }
                    this._statistics.Add(new Stats.FloatStatInfo()
                    {
                        Id = floatStat.Id,
                        DisplayName = floatStat.DisplayName,
                        FloatValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = floatStat.IncrementOnly,
                        Permission = floatStat.Permission,
                    });
                }
            }
        }

        /// <summary>
        /// Queues an achievement icon for download or loads it from cache if available.
        /// </summary>
        private void QueueAchievementIcon(Stats.AchievementInfo info, bool startDownload)
        {
            var key = info.Id + "_" + (info.IsAchieved == true ? "achieved" : "locked");
            int imageIndex = this._AchievementImageList.Images.IndexOfKey(key);

            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
                return;
            }

            var icon = info.IsAchieved == true ? info.IconNormal : info.IconLocked;
            if (string.IsNullOrEmpty(icon))
            {
                info.ImageIndex = 0;
                return;
            }

            if (this._achievementIconManager.TryLoadFromCache(info, out var cachedImage))
            {
                this.AddAchievementIcon(info, cachedImage);
                return;
            }

            this._achievementIconManager.QueueIcon(info);

            if (startDownload == true)
            {
                this.DownloadNextIcon();
            }
        }

        private int StoreAchievements(bool silent = false)
        {
            if (this._AchievementListView.Items.Count == 0)
            {
                return 0;
            }

            List<Stats.AchievementInfo> achievements = new();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                if (item.Tag is not Stats.AchievementInfo achievementInfo ||
                    achievementInfo.IsAchieved == item.Checked)
                {
                    continue;
                }

                achievementInfo.IsAchieved = item.Checked;
                achievements.Add(achievementInfo);
            }

            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (var info in achievements)
            {
                if (this._SteamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    if (!silent)
                    {
                        MessageBox.Show(
                            this,
                            $"An error occurred while setting the state for {info.Id}, aborting store.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    return -1;
                }
            }

            return achievements.Count;
        }

        private int StoreStatistics(bool silent = false)
        {
            if (this._statistics.Count == 0)
            {
                return 0;
            }

            var statistics = this._statistics.Where(stat => stat.IsModified == true).ToList();
            if (statistics.Count == 0)
            {
                return 0;
            }

            foreach (var stat in statistics)
            {
                if (stat is Stats.IntStatInfo intStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        if (!silent)
                        {
                            MessageBox.Show(
                                this,
                                $"An error occurred while setting the value for {stat.Id}, aborting store.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                        return -1;
                    }
                }
                else if (stat is Stats.FloatStatInfo floatStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        if (!silent)
                        {
                            MessageBox.Show(
                                this,
                                $"An error occurred while setting the value for {stat.Id}, aborting store.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                        return -1;
                    }
                }
                else
                {
                    throw new InvalidOperationException("unsupported stat type");
                }
            }

            return statistics.Count;
        }

        private void DisableInput()
        {
            this._ReloadButton.Enabled = false;
            this._StoreButton.Enabled = false;
        }

        private void EnableInput()
        {
            this._ReloadButton.Enabled = true;
            this._StoreButton.Enabled = true;
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this.RefreshStats();
        }

        private void OnLockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = false;
            }
        }

        private void OnInvertAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = !item.Checked;
            }
        }

        private void OnUnlockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = true;
            }
        }

        private bool Store()
        {
            if (this._SteamClient.SteamUserStats.StoreStats() == false)
            {
                MessageBox.Show(
                    this,
                    "An error occurred while storing, aborting.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
        private void PerformStore(bool silent = false)
        {
            int achievements = this.StoreAchievements(silent);
            if (achievements < 0)
            {
                if (!silent)
                {
                    this.RefreshStats();
                }
                return;
            }

            int stats = this.StoreStatistics(silent);
            if (stats < 0)
            {
                if (!silent)
                {
                    this.RefreshStats();
                }
                return;
            }

            if (this.Store() == false)
            {
                if (!silent)
                {
                    this.RefreshStats();
                }
                return;
            }

            if (!silent)
            {
                MessageBox.Show(
                    this,
                    $"Stored {achievements} achievements and {stats} statistics.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                this.RefreshStats(); // Silent mode still refreshes stats
            }
        }
        private void OnStore(object sender, EventArgs e)
        {
            PerformStore(false);
        }

        private void OnStatDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context != DataGridViewDataErrorContexts.Commit)
            {
                return;
            }

            var view = (DataGridView)sender;
            if (e.Exception is Stats.StatIsProtectedException)
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Stat is protected! -- you can't modify it";
            }
            else
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Invalid value";
            }
        }

        private void OnStatAgreementChecked(object sender, EventArgs e)
        {
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
        }

        private void OnStatCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var view = (DataGridView)sender;
            view.Rows[e.RowIndex].ErrorText = "";
        }

        private void OnResetAllStats(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you absolutely sure you want to reset stats?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            bool achievementsToo = DialogResult.Yes == MessageBox.Show(
                "Do you want to reset achievements too?",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (MessageBox.Show(
                "Really really sure?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error) == DialogResult.No)
            {
                return;
            }

            if (this._SteamClient.SteamUserStats.ResetAllStats(achievementsToo) == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.RefreshStats();
        }

        private void OnCheckAchievement(object sender, ItemCheckEventArgs e)
        {
            if (sender != this._AchievementListView)
            {
                return;
            }

            if (this._IsUpdatingAchievementList == true)
            {
                return;
            }

            if (this._AchievementListView.Items[e.Index].Tag is not Stats.AchievementInfo info)
            {
                return;
            }

            if ((info.Permission & 3) != 0)
            {
                MessageBox.Show(
                    this,
                    "Sorry, but this is a protected achievement and cannot be managed with Steam Achievement Manager.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                e.NewValue = e.CurrentValue;
            }
        }

        private void OnDisplayUncheckedOnly(object sender, EventArgs e)
        {
            if (sender is ToolStripButton button && button.Checked)
            {
                this._DisplayLockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnDisplayCheckedOnly(object sender, EventArgs e)
        {
            if (sender is ToolStripButton button && button.Checked)
            {
                this._DisplayUnlockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnFilterUpdate(object sender, KeyEventArgs e)
        {
            this.GetAchievements();
        }

        private void _timeNowTimer_Tick(object sender, EventArgs e)
        {
            _TimeNowLabel.Text = "   Cur. Time: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }
        private bool IsInteger(string text)
        {
            int result;
            return int.TryParse(text, out result);
        }
        private void _AddTimerTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_AddTimerTextBox.Text.Length > MaxTimerTextLength)
                _AddTimerTextBox.Text = _AddTimerTextBox.Text.Substring(0, MaxTimerTextLength);

            if (_AddTimerTextBox.Text != "-" && _AddTimerTextBox.Text != "" && !IsInteger(_AddTimerTextBox.Text))
            {
                _AddTimerTextBox.Text = "-1";
                _AddTimerTextBox.SelectionStart = _AddTimerTextBox.Text.Length;
            }
        }

        private void _AddTimerTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '-')
            {
                if (_AddTimerTextBox.SelectionStart == 0 && !_AddTimerTextBox.Text.Contains("-"))
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
            else if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void _TimerSwitchButton_Click(object sender, EventArgs e)
        {
            // Toggle the timer's Enabled state
            _submitAchievementsTimer.Enabled = !_submitAchievementsTimer.Enabled;

            // Update the button's text to reflect the new state
            UpdateButtonText();
        }

        private void _AddTimerButton_Click(object sender, EventArgs e)
        {
            // Ensure the value in _AddTimerTextBox is a valid number
            if (!int.TryParse(_AddTimerTextBox.Text, out int timerValue))
            {
                MessageBox.Show("Please enter a valid number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if any rows are selected in the ListView
            if (_AchievementListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one row!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update the fifth column of the selected rows with the specified value
            foreach (ListViewItem item in _AchievementListView.SelectedItems)
            {
                item.SubItems[4].Text = timerValue.ToString(); // Fifth column (Display Index 4)

                string key = item.SubItems[3].Text;
                _achievementCounters[key] = timerValue; // Store value for refresh
            }

            //MessageBox.Show("Selected rows have been successfully updated!", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateButtonText()
        {
            // Change the button's text based on the timer's current state
            if (_submitAchievementsTimer.Enabled)
            {
                _TimerSwitchButton.Text = "Disable Timer";
            }
            else
            {
                _TimerSwitchButton.Text = "Enable Timer";
            }
        }

        private void UpdateAchievementItem(ListViewItem item, ref bool shouldTriggerStore)
        {
            // Get the Key (3rd column) and Counter (4th column)
            string key = item.SubItems[3].Text; // 3rd column is Key

            string valueText = item.SubItems[4].Text; // 4th column is Counter

            if (int.TryParse(valueText, out int counter) && counter > 0)
            {
                counter -= 1;

                // Update the Counter column in ListView
                item.SubItems[4].Text = counter.ToString();

                // Update the Dictionary
                _achievementCounters[key] = counter;

                // If the counter becomes 0, check the row and set the flag
                if (counter == 0)
                {
                    item.Checked = true; // Check the row
                    shouldTriggerStore = true; // Set the flag to trigger the store action

                    item.SubItems[4].Text = "-1000";
                    _achievementCounters[key] = -1; // Update the dictionary as well
                }
            }
        }

        private void _submitAchievementsTimer_Tick(object sender, EventArgs e)
        {
            bool shouldTriggerStore = false; // Flag to determine if we need to trigger the commit button
            int seconds = DateTime.Now.Second;

            _TimerLabel.Text = (seconds % 2 == 0) ? "*" : "-";

            try
            {
                _AchievementListView.BeginUpdate();
                foreach (ListViewItem item in _AchievementListView.Items)
                {
                    UpdateAchievementItem(item, ref shouldTriggerStore);
                }

                // Trigger the store process only once if necessary
                if (shouldTriggerStore)
                {
                    PerformStore(true); // Silent mode
                }
            }
            finally
            {
                _AchievementListView.EndUpdate();
            }
        }
        private void MoveMouseIfNeeded()
        {
            GetCursorPos(out POINT currentPos);
            if (currentPos.X == _lastMousePos.X && currentPos.Y == _lastMousePos.Y)
            {
                int newX = _moveRight ? currentPos.X + MouseMoveDistance : currentPos.X - MouseMoveDistance;
                for (int i = 0; i < MouseMoveDistance; i++)
                {
                    int intermediateX = _moveRight ? currentPos.X + i : currentPos.X - i;
                    SetCursorPos(intermediateX, currentPos.Y);
                    mouse_event(MOUSEEVENTF_MOVE, (uint)(intermediateX - currentPos.X), 0, 0, UIntPtr.Zero);
                    Thread.Sleep(MouseMoveDelayMs);
                }
                _moveRight = !_moveRight;
            }
            GetCursorPos(out _lastMousePos); // Update last mouse position
        }

        private void PreventSleep()
        {
            SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_DISPLAY_REQUIRED | ExecutionState.ES_SYSTEM_REQUIRED);
        }

        private bool IsForeground()
        {
            return this == Form.ActiveForm;
        }

        private void _idleTimer_Tick(object sender, EventArgs e)
        {
            if (IsForeground())
            {
                MoveMouseIfNeeded();
                PreventSleep();
            }
        }

        private void _autoMouseMoveButton_Click(object sender, EventArgs e)
        {
            _isAutoMouseMoveEnabled = !_isAutoMouseMoveEnabled;
            if (_isAutoMouseMoveEnabled)
            {
                _idleTimer.Start();
                _autoMouseMoveButton.Text = "Stop Auto Mouse Move";
            }
            else
            {
                _idleTimer.Stop();
                _autoMouseMoveButton.Text = "Start Auto Mouse Move";
            }
        }

        // Track the current column and sort order for the achievement list
        private int sortColumn = 0; // default to name column
        private SortOrder sortOrder = SortOrder.Ascending;

        private void AchievementListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            _AchievementListView.BeginUpdate();
            _AchievementListView.SuspendLayout();
            try
            {
                if (e.Column != sortColumn)
                {
                    sortColumn = e.Column;
                    sortOrder = SortOrder.Ascending;
                }
                else
                {
                    sortOrder = (sortOrder == SortOrder.Ascending)
                        ? SortOrder.Descending
                        : SortOrder.Ascending;
                }

                _AchievementListView.ListViewItemSorter = new ListViewItemComparer(e.Column, sortOrder);
                _AchievementListView.Sort();
            }
            finally
            {
                _AchievementListView.ResumeLayout();
                _AchievementListView.EndUpdate();
            }
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
                WinForms.DwmWindowManager.ApplyMicaEffect(this.Handle, !WinForms.WindowsThemeDetector.IsLightTheme());
            }
        }

        private void OnDragWindow(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if ((ReferenceEquals(sender, this._MainToolStrip) && this._MainToolStrip.GetItemAt(e.Location) != null) ||
                    (ReferenceEquals(sender, this._AchievementsToolStrip) && this._AchievementsToolStrip.GetItemAt(e.Location) != null))
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

            Color restrictedBack = light
                ? ControlPaint.Light(this._AchievementListView.BackColor)
                : ControlPaint.Dark(this._AchievementListView.BackColor);
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                bool restricted = item.Tag is Stats.AchievementInfo info && (info.Permission & 3) != 0;
                Color itemBack = restricted ? restrictedBack : this._AchievementListView.BackColor;
                ThemeHelper.ApplyTheme(item, itemBack, this._AchievementListView.ForeColor);
            }

            this.Invalidate();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }
        class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int col;
            private readonly SortOrder order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem itemX || y is not ListViewItem itemY)
                {
                    return 0;
                }

                var s1 = itemX.SubItems[col].Text;
                var s2 = itemY.SubItems[col].Text;

                int result;

                // Try to parse as DateTime for date comparison
                if (DateTime.TryParse(s1, out DateTime d1) && DateTime.TryParse(s2, out DateTime d2))
                {
                    result = DateTime.Compare(d1, d2);
                }
                else
                {
                    // Default to string comparison
                    result = String.Compare(s1, s2);
                }

                return (order == SortOrder.Ascending) ? result : -result;
            }
        }
    }
}
