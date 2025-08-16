namespace SAM.Game
{
    partial class Manager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.Windows.Forms.ToolStripSeparator _ToolStripSeparator1;
            System.Windows.Forms.ToolStripSeparator _ToolStripSeparator2;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Manager));
            _MainToolStrip = new System.Windows.Forms.ToolStrip();
            _ReloadButton = new System.Windows.Forms.ToolStripButton();
            _ResetButton = new System.Windows.Forms.ToolStripButton();
            _TimeNowLabel = new System.Windows.Forms.ToolStripLabel();
            _TimerLabel = new System.Windows.Forms.ToolStripLabel();
            _CloseButton = new System.Windows.Forms.ToolStripButton();
            _StoreButton = new System.Windows.Forms.ToolStripButton();
            _autoMouseMoveButton = new System.Windows.Forms.ToolStripButton();
            _AchievementImageList = new System.Windows.Forms.ImageList(components);
            _MainStatusStrip = new System.Windows.Forms.StatusStrip();
            _CountryStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _GameStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _DownloadStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _CallbackTimer = new System.Windows.Forms.Timer(components);
            _MainTabControl = new System.Windows.Forms.TabControl();
            _AchievementsTabPage = new System.Windows.Forms.TabPage();
            _AchievementListView = new DoubleBufferedListView();
            _AchievementNameColumnHeader = new System.Windows.Forms.ColumnHeader();
            _AchievementDescriptionColumnHeader = new System.Windows.Forms.ColumnHeader();
            _AchievementUnlockTimeColumnHeader = new System.Windows.Forms.ColumnHeader();
            _AchievementIDColumnHeader = new System.Windows.Forms.ColumnHeader();
            _AchievementTimerColumnHeader = new System.Windows.Forms.ColumnHeader();
            _AchievementsToolStrip = new System.Windows.Forms.ToolStrip();
            _LockAllButton = new System.Windows.Forms.ToolStripButton();
            _InvertAllButton = new System.Windows.Forms.ToolStripButton();
            _UnlockAllButton = new System.Windows.Forms.ToolStripButton();
            _DisplayLabel = new System.Windows.Forms.ToolStripLabel();
            _DisplayLockedOnlyButton = new System.Windows.Forms.ToolStripButton();
            _DisplayUnlockedOnlyButton = new System.Windows.Forms.ToolStripButton();
            _MatchingStringLabel = new System.Windows.Forms.ToolStripLabel();
            _MatchingStringTextBox = new System.Windows.Forms.ToolStripTextBox();
            _LanguageLabel = new System.Windows.Forms.ToolStripLabel();
            _LanguageComboBox = new System.Windows.Forms.ToolStripComboBox();
            AddTimerLabel = new System.Windows.Forms.ToolStripLabel();
            _AddTimerTextBox = new System.Windows.Forms.ToolStripTextBox();
            _AddTimerButton = new System.Windows.Forms.ToolStripButton();
            _TimerSwitchButton = new System.Windows.Forms.ToolStripButton();
            _StatisticsTabPage = new System.Windows.Forms.TabPage();
            _EnableStatsEditingCheckBox = new System.Windows.Forms.CheckBox();
            _StatisticsDataGridView = new System.Windows.Forms.DataGridView();
            _timeNowTimer = new System.Windows.Forms.Timer(components);
            _submitAchievementsTimer = new System.Windows.Forms.Timer(components);
            _idleTimer = new System.Windows.Forms.Timer(components);
            _ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _ToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            _MainToolStrip.SuspendLayout();
            _MainStatusStrip.SuspendLayout();
            _MainTabControl.SuspendLayout();
            _AchievementsTabPage.SuspendLayout();
            _AchievementsToolStrip.SuspendLayout();
            _StatisticsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_StatisticsDataGridView).BeginInit();
            SuspendLayout();
            // 
            // _ToolStripSeparator1
            // 
            _ToolStripSeparator1.Name = "_ToolStripSeparator1";
            _ToolStripSeparator1.Size = new System.Drawing.Size(6, 45);
            // 
            // _ToolStripSeparator2
            // 
            _ToolStripSeparator2.Name = "_ToolStripSeparator2";
            _ToolStripSeparator2.Size = new System.Drawing.Size(6, 45);
            // 
            // _MainToolStrip
            // 
            _MainToolStrip.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _MainToolStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _MainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _ReloadButton, _ResetButton, _TimeNowLabel, _TimerLabel, _CloseButton, _StoreButton, _autoMouseMoveButton });
            _MainToolStrip.Location = new System.Drawing.Point(0, 0);
            _MainToolStrip.Name = "_MainToolStrip";
            _MainToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            _MainToolStrip.Size = new System.Drawing.Size(1778, 45);
            _MainToolStrip.TabIndex = 1;
            // 
            // _ReloadButton
            // 
            _ReloadButton.Enabled = false;
            _ReloadButton.Image = Resources.RefreshN;
            _ReloadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _ReloadButton.Name = "_ReloadButton";
            _ReloadButton.Size = new System.Drawing.Size(141, 39);
            _ReloadButton.Text = "Refresh";
            _ReloadButton.ToolTipText = "Refresh achievements and statistics for active game.";
            _ReloadButton.Click += OnRefresh;
            // 
            // _ResetButton
            // 
            _ResetButton.Image = Resources.cancel;
            _ResetButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _ResetButton.Name = "_ResetButton";
            _ResetButton.Size = new System.Drawing.Size(116, 39);
            _ResetButton.Text = "Reset";
            _ResetButton.ToolTipText = "Reset achievements and/or statistics for active game.";
            _ResetButton.Click += OnResetAllStats;
            // 
            // _TimeNowLabel
            // 
            _TimeNowLabel.Image = (System.Drawing.Image)resources.GetObject("_TimeNowLabel.Image");
            _TimeNowLabel.Name = "_TimeNowLabel";
            _TimeNowLabel.Size = new System.Drawing.Size(327, 39);
            _TimeNowLabel.Text = "Time: ____/__/__ __:__:__";
            // 
            // _TimerLabel
            // 
            _TimerLabel.Name = "_TimerLabel";
            _TimerLabel.Size = new System.Drawing.Size(27, 39);
            _TimerLabel.Text = "-";
            // 
            // _CloseButton
            // 
            _CloseButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _CloseButton.AutoSize = false;
            _CloseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _CloseButton.Image = Resources.cross;
            _CloseButton.Name = "_CloseButton";
            _CloseButton.Size = new System.Drawing.Size(45, 39);
            _CloseButton.Text = "X";
            _CloseButton.Click += OnCloseButtonClick;
            // 
            // _StoreButton
            // 
            _StoreButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _StoreButton.Enabled = false;
            _StoreButton.Image = Resources.cloud_computing;
            _StoreButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _StoreButton.Name = "_StoreButton";
            _StoreButton.Size = new System.Drawing.Size(265, 39);
            _StoreButton.Text = "Commit Changes";
            _StoreButton.ToolTipText = "Store achievements and statistics for active game.";
            _StoreButton.Click += OnStore;
            // 
            // _autoMouseMoveButton
            // 
            _autoMouseMoveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _autoMouseMoveButton.Image = Resources.cursor;
            _autoMouseMoveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _autoMouseMoveButton.Name = "_autoMouseMoveButton";
            _autoMouseMoveButton.Size = new System.Drawing.Size(348, 39);
            _autoMouseMoveButton.Text = "Start Auto Mouse Move";
            _autoMouseMoveButton.ToolTipText = "Auto mouse movement/every 60 seconds when program in foreground";
            _autoMouseMoveButton.Click += _autoMouseMoveButton_Click;
            // 
            // _AchievementImageList
            // 
            _AchievementImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            _AchievementImageList.ImageSize = new System.Drawing.Size(64, 64);
            _AchievementImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // _MainStatusStrip
            // 
            _MainStatusStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _MainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _CountryStatusLabel, _GameStatusLabel, _DownloadStatusLabel });
            _MainStatusStrip.Location = new System.Drawing.Point(0, 762);
            _MainStatusStrip.Name = "_MainStatusStrip";
            _MainStatusStrip.Padding = new System.Windows.Forms.Padding(3, 0, 33, 0);
            _MainStatusStrip.Size = new System.Drawing.Size(1778, 22);
            _MainStatusStrip.TabIndex = 4;
            // 
            // _CountryStatusLabel
            // 
            _CountryStatusLabel.Name = "_CountryStatusLabel";
            _CountryStatusLabel.Size = new System.Drawing.Size(0, 13);
            // 
            // _GameStatusLabel
            // 
            _GameStatusLabel.Name = "_GameStatusLabel";
            _GameStatusLabel.Size = new System.Drawing.Size(1742, 13);
            _GameStatusLabel.Spring = true;
            _GameStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _DownloadStatusLabel
            // 
            _DownloadStatusLabel.Image = Resources.Download;
            _DownloadStatusLabel.Name = "_DownloadStatusLabel";
            _DownloadStatusLabel.Size = new System.Drawing.Size(207, 28);
            _DownloadStatusLabel.Text = "Download status";
            _DownloadStatusLabel.Visible = false;
            // 
            // _CallbackTimer
            // 
            _CallbackTimer.Enabled = true;
            _CallbackTimer.Tick += OnTimer;
            // 
            // _MainTabControl
            // 
            _MainTabControl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _MainTabControl.Controls.Add(_AchievementsTabPage);
            _MainTabControl.Controls.Add(_StatisticsTabPage);
            _MainTabControl.Location = new System.Drawing.Point(19, 66);
            _MainTabControl.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _MainTabControl.Name = "_MainTabControl";
            _MainTabControl.SelectedIndex = 0;
            _MainTabControl.Size = new System.Drawing.Size(1741, 669);
            _MainTabControl.TabIndex = 5;
            // 
            // _AchievementsTabPage
            // 
            _AchievementsTabPage.Controls.Add(_AchievementListView);
            _AchievementsTabPage.Controls.Add(_AchievementsToolStrip);
            _AchievementsTabPage.Location = new System.Drawing.Point(4, 36);
            _AchievementsTabPage.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _AchievementsTabPage.Name = "_AchievementsTabPage";
            _AchievementsTabPage.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _AchievementsTabPage.Size = new System.Drawing.Size(1733, 629);
            _AchievementsTabPage.TabIndex = 0;
            _AchievementsTabPage.Text = "Achievements";
            _AchievementsTabPage.UseVisualStyleBackColor = true;
            // 
            // _AchievementListView
            // 
            _AchievementListView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            _AchievementListView.BackgroundImageTiled = true;
            _AchievementListView.CheckBoxes = true;
            _AchievementListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _AchievementNameColumnHeader, _AchievementDescriptionColumnHeader, _AchievementUnlockTimeColumnHeader, _AchievementIDColumnHeader, _AchievementTimerColumnHeader });
            _AchievementListView.Dock = System.Windows.Forms.DockStyle.Fill;
            _AchievementListView.FullRowSelect = true;
            _AchievementListView.GridLines = true;
            _AchievementListView.LargeImageList = _AchievementImageList;
            _AchievementListView.Location = new System.Drawing.Point(8, 51);
            _AchievementListView.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _AchievementListView.Name = "_AchievementListView";
            _AchievementListView.Size = new System.Drawing.Size(1717, 572);
            _AchievementListView.SmallImageList = _AchievementImageList;
            _AchievementListView.TabIndex = 4;
            _AchievementListView.UseCompatibleStateImageBehavior = false;
            _AchievementListView.View = System.Windows.Forms.View.Details;
            _AchievementListView.ColumnClick += AchievementListView_ColumnClick;
            _AchievementListView.ItemCheck += OnCheckAchievement;
            // 
            // _AchievementNameColumnHeader
            // 
            _AchievementNameColumnHeader.Text = "Name";
            _AchievementNameColumnHeader.Width = 200;
            // 
            // _AchievementDescriptionColumnHeader
            // 
            _AchievementDescriptionColumnHeader.Text = "Description";
            _AchievementDescriptionColumnHeader.Width = 300;
            // 
            // _AchievementUnlockTimeColumnHeader
            // 
            _AchievementUnlockTimeColumnHeader.Text = "Unlock Time";
            _AchievementUnlockTimeColumnHeader.Width = 160;
            // 
            // _AchievementIDColumnHeader
            // 
            _AchievementIDColumnHeader.Text = "ID";
            _AchievementIDColumnHeader.Width = 5;
            // 
            // _AchievementTimerColumnHeader
            // 
            _AchievementTimerColumnHeader.Text = "Commit Timer";
            _AchievementTimerColumnHeader.Width = 208;
            // 
            // _AchievementsToolStrip
            // 
            _AchievementsToolStrip.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _AchievementsToolStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _AchievementsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _LockAllButton, _InvertAllButton, _UnlockAllButton, _ToolStripSeparator1, _DisplayLabel, _DisplayLockedOnlyButton, _DisplayUnlockedOnlyButton, _ToolStripSeparator2, _MatchingStringLabel, _MatchingStringTextBox, _LanguageLabel, _LanguageComboBox, AddTimerLabel, _AddTimerTextBox, _AddTimerButton, _TimerSwitchButton });
            _AchievementsToolStrip.Location = new System.Drawing.Point(8, 6);
            _AchievementsToolStrip.Name = "_AchievementsToolStrip";
            _AchievementsToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            _AchievementsToolStrip.Size = new System.Drawing.Size(1717, 45);
            _AchievementsToolStrip.TabIndex = 5;
            // 
            // _LockAllButton
            // 
            _LockAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _LockAllButton.Image = Resources.padlock;
            _LockAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _LockAllButton.Name = "_LockAllButton";
            _LockAllButton.Size = new System.Drawing.Size(40, 39);
            _LockAllButton.Text = "Lock All";
            _LockAllButton.ToolTipText = "Lock all achievements.";
            _LockAllButton.Click += OnLockAll;
            // 
            // _InvertAllButton
            // 
            _InvertAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _InvertAllButton.Image = Resources.invertN;
            _InvertAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _InvertAllButton.Name = "_InvertAllButton";
            _InvertAllButton.Size = new System.Drawing.Size(40, 39);
            _InvertAllButton.Text = "Invert All";
            _InvertAllButton.ToolTipText = "Invert all achievements.";
            _InvertAllButton.Click += OnInvertAll;
            // 
            // _UnlockAllButton
            // 
            _UnlockAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _UnlockAllButton.Image = Resources.unlockN;
            _UnlockAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _UnlockAllButton.Name = "_UnlockAllButton";
            _UnlockAllButton.Size = new System.Drawing.Size(40, 39);
            _UnlockAllButton.Text = "Unlock All";
            _UnlockAllButton.ToolTipText = "Unlock all achievements.";
            _UnlockAllButton.Click += OnUnlockAll;
            // 
            // _DisplayLabel
            // 
            _DisplayLabel.Name = "_DisplayLabel";
            _DisplayLabel.Size = new System.Drawing.Size(90, 39);
            _DisplayLabel.Text = "Show:";
            // 
            // _DisplayLockedOnlyButton
            // 
            _DisplayLockedOnlyButton.CheckOnClick = true;
            _DisplayLockedOnlyButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            _DisplayLockedOnlyButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _DisplayLockedOnlyButton.Name = "_DisplayLockedOnlyButton";
            _DisplayLockedOnlyButton.Size = new System.Drawing.Size(102, 39);
            _DisplayLockedOnlyButton.Text = "locked";
            _DisplayLockedOnlyButton.Click += OnDisplayCheckedOnly;
            // 
            // _DisplayUnlockedOnlyButton
            // 
            _DisplayUnlockedOnlyButton.CheckOnClick = true;
            _DisplayUnlockedOnlyButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            _DisplayUnlockedOnlyButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _DisplayUnlockedOnlyButton.Name = "_DisplayUnlockedOnlyButton";
            _DisplayUnlockedOnlyButton.Size = new System.Drawing.Size(134, 39);
            _DisplayUnlockedOnlyButton.Text = "unlocked";
            _DisplayUnlockedOnlyButton.Click += OnDisplayUncheckedOnly;
            // 
            // _MatchingStringLabel
            // 
            _MatchingStringLabel.Name = "_MatchingStringLabel";
            _MatchingStringLabel.Size = new System.Drawing.Size(78, 39);
            _MatchingStringLabel.Text = "Filter";
            // 
            // _MatchingStringTextBox
            // 
            _MatchingStringTextBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _MatchingStringTextBox.Name = "_MatchingStringTextBox";
            _MatchingStringTextBox.Size = new System.Drawing.Size(180, 45);
            _MatchingStringTextBox.ToolTipText = "Type at least 3 characters that must appear in the name or description";
            _MatchingStringTextBox.KeyUp += OnFilterUpdate;
            // 
            // _LanguageLabel
            // 
            _LanguageLabel.Name = "_LanguageLabel";
            _LanguageLabel.Size = new System.Drawing.Size(140, 39);
            _LanguageLabel.Text = "Language";
            // 
            // _LanguageComboBox
            // 
            _LanguageComboBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _LanguageComboBox.Items.AddRange(new object[] { "english", "tchinese", "japanese", "arabic", "brazilian", "bulgarian", "czech", "danish", "dutch", "finnish", "french", "german", "greek", "hungarian", "indonesian", "italian", "koreana", "latam", "norwegian", "polish", "portuguese", "romanian", "russian", "schinese", "spanish", "swedish", "th", "turkish", "ukrainian", "vietnamese" });
            _LanguageComboBox.Name = "_LanguageComboBox";
            _LanguageComboBox.Size = new System.Drawing.Size(160, 45);
            // 
            // AddTimerLabel
            // 
            AddTimerLabel.Name = "AddTimerLabel";
            AddTimerLabel.Size = new System.Drawing.Size(123, 39);
            AddTimerLabel.Text = "Counter:";
            // 
            // _AddTimerTextBox
            // 
            _AddTimerTextBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _AddTimerTextBox.Name = "_AddTimerTextBox";
            _AddTimerTextBox.Size = new System.Drawing.Size(120, 45);
            _AddTimerTextBox.Text = "600";
            _AddTimerTextBox.KeyPress += _AddTimerTextBox_KeyPress;
            _AddTimerTextBox.TextChanged += _AddTimerTextBox_TextChanged;
            // 
            // _AddTimerButton
            // 
            _AddTimerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            _AddTimerButton.Image = (System.Drawing.Image)resources.GetObject("_AddTimerButton.Image");
            _AddTimerButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _AddTimerButton.Name = "_AddTimerButton";
            _AddTimerButton.Size = new System.Drawing.Size(181, 39);
            _AddTimerButton.Text = "Add Counter";
            _AddTimerButton.Click += _AddTimerButton_Click;
            // 
            // _TimerSwitchButton
            // 
            _TimerSwitchButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            _TimerSwitchButton.Image = (System.Drawing.Image)resources.GetObject("_TimerSwitchButton.Image");
            _TimerSwitchButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _TimerSwitchButton.Name = "_TimerSwitchButton";
            _TimerSwitchButton.Size = new System.Drawing.Size(158, 39);
            _TimerSwitchButton.Text = "Start Timer";
            _TimerSwitchButton.ToolTipText = "Start countdown timer";
            _TimerSwitchButton.Click += _TimerSwitchButton_Click;
            // 
            // _StatisticsTabPage
            // 
            _StatisticsTabPage.Controls.Add(_EnableStatsEditingCheckBox);
            _StatisticsTabPage.Controls.Add(_StatisticsDataGridView);
            _StatisticsTabPage.Location = new System.Drawing.Point(4, 35);
            _StatisticsTabPage.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _StatisticsTabPage.Name = "_StatisticsTabPage";
            _StatisticsTabPage.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _StatisticsTabPage.Size = new System.Drawing.Size(1733, 630);
            _StatisticsTabPage.TabIndex = 1;
            _StatisticsTabPage.Text = "Statistics";
            _StatisticsTabPage.UseVisualStyleBackColor = true;
            // 
            // _EnableStatsEditingCheckBox
            // 
            _EnableStatsEditingCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _EnableStatsEditingCheckBox.AutoSize = true;
            _EnableStatsEditingCheckBox.Location = new System.Drawing.Point(14, 563);
            _EnableStatsEditingCheckBox.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _EnableStatsEditingCheckBox.Name = "_EnableStatsEditingCheckBox";
            _EnableStatsEditingCheckBox.Size = new System.Drawing.Size(1112, 31);
            _EnableStatsEditingCheckBox.TabIndex = 1;
            _EnableStatsEditingCheckBox.Text = "I understand by modifying the values of stats, I may screw things up and can't blame anyone but myself.";
            _EnableStatsEditingCheckBox.UseVisualStyleBackColor = true;
            _EnableStatsEditingCheckBox.CheckedChanged += OnStatAgreementChecked;
            // 
            // _StatisticsDataGridView
            // 
            _StatisticsDataGridView.AllowUserToAddRows = false;
            _StatisticsDataGridView.AllowUserToDeleteRows = false;
            _StatisticsDataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _StatisticsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _StatisticsDataGridView.Location = new System.Drawing.Point(14, 12);
            _StatisticsDataGridView.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _StatisticsDataGridView.Name = "_StatisticsDataGridView";
            _StatisticsDataGridView.RowHeadersWidth = 72;
            _StatisticsDataGridView.Size = new System.Drawing.Size(1391, 537);
            _StatisticsDataGridView.TabIndex = 0;
            _StatisticsDataGridView.CellEndEdit += OnStatCellEndEdit;
            _StatisticsDataGridView.DataError += OnStatDataError;
            // 
            // _timeNowTimer
            //
            _timeNowTimer.Enabled = true;
            _timeNowTimer.Interval = 500;
            _timeNowTimer.Tick += _timeNowTimer_Tick;
            // 
            // _submitAchievementsTimer
            // 
            _submitAchievementsTimer.Interval = 1000;
            _submitAchievementsTimer.Tick += _submitAchievementsTimer_Tick;
            // 
            // _idleTimer
            // 
            _idleTimer.Interval = 60000;
            _idleTimer.Tick += _idleTimer_Tick;
            // 
            // Manager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1778, 784);
            Controls.Add(_MainToolStrip);
            Controls.Add(_MainTabControl);
            Controls.Add(_MainStatusStrip);
            Font = new System.Drawing.Font("新細明體", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            MinimumSize = new System.Drawing.Size(1461, 64);
            Name = "Manager";
            Text = "Steam Achievement ManagerX 7.0";
            _MainToolStrip.ResumeLayout(false);
            _MainToolStrip.PerformLayout();
            _MainStatusStrip.ResumeLayout(false);
            _MainStatusStrip.PerformLayout();
            _MainTabControl.ResumeLayout(false);
            _AchievementsTabPage.ResumeLayout(false);
            _AchievementsTabPage.PerformLayout();
            _AchievementsToolStrip.ResumeLayout(false);
            _AchievementsToolStrip.PerformLayout();
            _StatisticsTabPage.ResumeLayout(false);
            _StatisticsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_StatisticsDataGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _MainToolStrip;
        private System.Windows.Forms.ToolStripButton _StoreButton;
        private System.Windows.Forms.ToolStripButton _ReloadButton;
        private System.Windows.Forms.StatusStrip _MainStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _CountryStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel _GameStatusLabel;
        private System.Windows.Forms.ImageList _AchievementImageList;
        private System.Windows.Forms.Timer _CallbackTimer;
        private System.Windows.Forms.TabControl _MainTabControl;
        private System.Windows.Forms.TabPage _AchievementsTabPage;
        private System.Windows.Forms.TabPage _StatisticsTabPage;
        private DoubleBufferedListView _AchievementListView;
        private System.Windows.Forms.ColumnHeader _AchievementNameColumnHeader;
        private System.Windows.Forms.ColumnHeader _AchievementDescriptionColumnHeader;
        private System.Windows.Forms.ToolStrip _AchievementsToolStrip;
        private System.Windows.Forms.ToolStripButton _LockAllButton;
        private System.Windows.Forms.ToolStripButton _InvertAllButton;
        private System.Windows.Forms.ToolStripButton _UnlockAllButton;
        private System.Windows.Forms.DataGridView _StatisticsDataGridView;
        private System.Windows.Forms.ToolStripButton _ResetButton;
        private System.Windows.Forms.ToolStripStatusLabel _DownloadStatusLabel;
        private System.Windows.Forms.ToolStripLabel _DisplayLabel;
        private System.Windows.Forms.ToolStripButton _DisplayUnlockedOnlyButton;
        private System.Windows.Forms.ToolStripButton _DisplayLockedOnlyButton;
        private System.Windows.Forms.ToolStripLabel _MatchingStringLabel;
        private System.Windows.Forms.ToolStripTextBox _MatchingStringTextBox;
        private System.Windows.Forms.ColumnHeader _AchievementUnlockTimeColumnHeader;
        private System.Windows.Forms.CheckBox _EnableStatsEditingCheckBox;
        private System.Windows.Forms.ToolStripLabel _LanguageLabel;
        private System.Windows.Forms.ToolStripComboBox _LanguageComboBox;
        private System.Windows.Forms.ColumnHeader _AchievementIDColumnHeader;
        private System.Windows.Forms.ColumnHeader _AchievementTimerColumnHeader;
        private System.Windows.Forms.ToolStripLabel _TimeNowLabel;
        private System.Windows.Forms.Timer _timeNowTimer;
        private System.Windows.Forms.ToolStripLabel AddTimerLabel;
        private System.Windows.Forms.ToolStripTextBox _AddTimerTextBox;
        private System.Windows.Forms.ToolStripButton _AddTimerButton;
        private System.Windows.Forms.ToolStripButton _TimerSwitchButton;
        private System.Windows.Forms.Timer _submitAchievementsTimer;
        private System.Windows.Forms.ToolStripLabel _TimerLabel;
        private System.Windows.Forms.Timer _idleTimer;
        private System.Windows.Forms.ToolStripButton _autoMouseMoveButton;
        private System.Windows.Forms.ToolStripButton _CloseButton;
    }
}
