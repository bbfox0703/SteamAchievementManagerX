namespace SAM.Picker
{
    partial class GamePicker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._LogoWorker != null && this._LogoWorker.IsBusy)
                {
                    this._LogoWorker.CancelAsync();
                    while (this._LogoWorker.IsBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }

                if (this._ListWorker != null && this._ListWorker.IsBusy)
                {
                    this._ListWorker.CancelAsync();
                    while (this._ListWorker.IsBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GamePicker));
            _LogoWorker = new System.ComponentModel.BackgroundWorker();
            _ListWorker = new System.ComponentModel.BackgroundWorker();
            _LogoImageList = new System.Windows.Forms.ImageList(components);
            _CallbackTimer = new System.Windows.Forms.Timer(components);
            _PickerToolStrip = new System.Windows.Forms.ToolStrip();
            _RefreshGamesButton = new System.Windows.Forms.ToolStripButton();
            _AddGameTextBox = new System.Windows.Forms.ToolStripTextBox();
            _AddGameButton = new System.Windows.Forms.ToolStripButton();
            _FindGamesLabel = new System.Windows.Forms.ToolStripLabel();
            _SearchGameTextBox = new System.Windows.Forms.ToolStripTextBox();
            _FilterDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            _FilterGamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            _FilterDemosMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            _FilterModsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            _FilterJunkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            _LanguageLabel = new System.Windows.Forms.ToolStripLabel();
            _LanguageComboBox = new System.Windows.Forms.ToolStripComboBox();
            _CloseButton = new System.Windows.Forms.ToolStripButton();
            _PickerStatusStrip = new System.Windows.Forms.StatusStrip();
            _PickerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _DownloadStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            _GameListView = new MyListView();
            _ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _ToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            _PickerToolStrip.SuspendLayout();
            _PickerStatusStrip.SuspendLayout();
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
            // _LogoWorker
            // 
            _LogoWorker.WorkerSupportsCancellation = true;
            _LogoWorker.DoWork += DoDownloadLogo;
            _LogoWorker.RunWorkerCompleted += OnDownloadLogo;
            // 
            // _ListWorker
            // 
            _ListWorker.WorkerSupportsCancellation = true;
            _ListWorker.DoWork += DoDownloadList;
            _ListWorker.RunWorkerCompleted += OnDownloadList;
            // 
            // _LogoImageList
            // 
            _LogoImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            _LogoImageList.ImageSize = new System.Drawing.Size(184, 69);
            _LogoImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // _CallbackTimer
            // 
            _CallbackTimer.Enabled = true;
            _CallbackTimer.Tick += OnTimer;
            // 
            // _PickerToolStrip
            // 
            _PickerToolStrip.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _PickerToolStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _PickerToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _RefreshGamesButton, _ToolStripSeparator1, _AddGameTextBox, _AddGameButton, _ToolStripSeparator2, _FindGamesLabel, _SearchGameTextBox, _FilterDropDownButton, _LanguageLabel, _LanguageComboBox, _CloseButton });
            _PickerToolStrip.Location = new System.Drawing.Point(0, 0);
            _PickerToolStrip.Name = "_PickerToolStrip";
            _PickerToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            _PickerToolStrip.Size = new System.Drawing.Size(1633, 45);
            _PickerToolStrip.TabIndex = 1;
            _PickerToolStrip.Text = "toolStrip1";
            // 
            // _RefreshGamesButton
            // 
            _RefreshGamesButton.Image = Resources.loading_arrow;
            _RefreshGamesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _RefreshGamesButton.Name = "_RefreshGamesButton";
            _RefreshGamesButton.Size = new System.Drawing.Size(235, 39);
            _RefreshGamesButton.Text = "Refresh Games";
            _RefreshGamesButton.Click += OnRefresh;
            // 
            // _AddGameTextBox
            // 
            _AddGameTextBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _AddGameTextBox.Name = "_AddGameTextBox";
            _AddGameTextBox.Size = new System.Drawing.Size(228, 45);
            // 
            // _AddGameButton
            // 
            _AddGameButton.Image = Resources.SearchN;
            _AddGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _AddGameButton.Name = "_AddGameButton";
            _AddGameButton.Size = new System.Drawing.Size(182, 39);
            _AddGameButton.Text = "Add Game";
            _AddGameButton.Click += OnAddGame;
            // 
            // _FindGamesLabel
            // 
            _FindGamesLabel.Name = "_FindGamesLabel";
            _FindGamesLabel.Size = new System.Drawing.Size(78, 39);
            _FindGamesLabel.Text = "Filter";
            // 
            // _SearchGameTextBox
            // 
            _SearchGameTextBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _SearchGameTextBox.Name = "_SearchGameTextBox";
            _SearchGameTextBox.Size = new System.Drawing.Size(228, 45);
            _SearchGameTextBox.KeyUp += OnFilterUpdate;
            // 
            // _FilterDropDownButton
            // 
            _FilterDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _FilterDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { _FilterGamesMenuItem, _FilterDemosMenuItem, _FilterModsMenuItem, _FilterJunkMenuItem });
            _FilterDropDownButton.Image = Resources.FilterN;
            _FilterDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _FilterDropDownButton.Name = "_FilterDropDownButton";
            _FilterDropDownButton.Size = new System.Drawing.Size(49, 39);
            _FilterDropDownButton.Text = "Game filtering";
            // 
            // _FilterGamesMenuItem
            // 
            _FilterGamesMenuItem.Checked = true;
            _FilterGamesMenuItem.CheckOnClick = true;
            _FilterGamesMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            _FilterGamesMenuItem.Name = "_FilterGamesMenuItem";
            _FilterGamesMenuItem.Size = new System.Drawing.Size(297, 44);
            _FilterGamesMenuItem.Text = "Show &games";
            _FilterGamesMenuItem.CheckedChanged += OnFilterUpdate;
            // 
            // _FilterDemosMenuItem
            // 
            _FilterDemosMenuItem.CheckOnClick = true;
            _FilterDemosMenuItem.Name = "_FilterDemosMenuItem";
            _FilterDemosMenuItem.Size = new System.Drawing.Size(297, 44);
            _FilterDemosMenuItem.Text = "Show &demos";
            _FilterDemosMenuItem.CheckedChanged += OnFilterUpdate;
            // 
            // _FilterModsMenuItem
            // 
            _FilterModsMenuItem.CheckOnClick = true;
            _FilterModsMenuItem.Name = "_FilterModsMenuItem";
            _FilterModsMenuItem.Size = new System.Drawing.Size(297, 44);
            _FilterModsMenuItem.Text = "Show &mods";
            _FilterModsMenuItem.CheckedChanged += OnFilterUpdate;
            // 
            // _FilterJunkMenuItem
            // 
            _FilterJunkMenuItem.CheckOnClick = true;
            _FilterJunkMenuItem.Name = "_FilterJunkMenuItem";
            _FilterJunkMenuItem.Size = new System.Drawing.Size(297, 44);
            _FilterJunkMenuItem.Text = "Show &junk";
            _FilterJunkMenuItem.CheckedChanged += OnFilterUpdate;
            // 
            // _LanguageLabel
            // 
            _LanguageLabel.Name = "_LanguageLabel";
            _LanguageLabel.Size = new System.Drawing.Size(140, 39);
            _LanguageLabel.Text = "Language";
            _LanguageLabel.Visible = false;
            // 
            // _LanguageComboBox
            // 
            _LanguageComboBox.Font = new System.Drawing.Font("Microsoft JhengHei UI", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            _LanguageComboBox.Items.AddRange(new object[] { "arabic", "brazilian", "bulgarian", "czech", "danish", "dutch", "english", "finnish", "french", "german", "greek", "hungarian", "indonesian", "italian", "japanese", "koreana", "latam", "norwegian", "polish", "portuguese", "romanian", "russian", "schinese", "spanish", "swedish", "tchinese", "th", "turkish", "ukrainian", "vietnamese" });
            _LanguageComboBox.Name = "_LanguageComboBox";
            _LanguageComboBox.Size = new System.Drawing.Size(160, 45);
            _LanguageComboBox.Visible = false;
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
            // _PickerStatusStrip
            // 
            _PickerStatusStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _PickerStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _PickerStatusLabel, _DownloadStatusLabel });
            _PickerStatusStrip.Location = new System.Drawing.Point(0, 618);
            _PickerStatusStrip.Name = "_PickerStatusStrip";
            _PickerStatusStrip.Padding = new System.Windows.Forms.Padding(3, 0, 33, 0);
            _PickerStatusStrip.Size = new System.Drawing.Size(1633, 22);
            _PickerStatusStrip.TabIndex = 2;
            _PickerStatusStrip.Text = "statusStrip";
            // 
            // _PickerStatusLabel
            // 
            _PickerStatusLabel.Name = "_PickerStatusLabel";
            _PickerStatusLabel.Size = new System.Drawing.Size(1597, 13);
            _PickerStatusLabel.Spring = true;
            _PickerStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _DownloadStatusLabel
            // 
            _DownloadStatusLabel.Image = Resources.Download;
            _DownloadStatusLabel.Name = "_DownloadStatusLabel";
            _DownloadStatusLabel.Size = new System.Drawing.Size(207, 28);
            _DownloadStatusLabel.Text = "Download status";
            _DownloadStatusLabel.Visible = false;
            // 
            // _GameListView
            // 
            _GameListView.BackColor = System.Drawing.Color.Black;
            _GameListView.Dock = System.Windows.Forms.DockStyle.Fill;
            _GameListView.ForeColor = System.Drawing.Color.White;
            _GameListView.LargeImageList = _LogoImageList;
            _GameListView.Location = new System.Drawing.Point(0, 45);
            _GameListView.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            _GameListView.MultiSelect = false;
            _GameListView.Name = "_GameListView";
            _GameListView.OwnerDraw = true;
            _GameListView.Size = new System.Drawing.Size(1633, 573);
            _GameListView.SmallImageList = _LogoImageList;
            _GameListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            _GameListView.TabIndex = 0;
            _GameListView.TileSize = new System.Drawing.Size(184, 69);
            _GameListView.UseCompatibleStateImageBehavior = false;
            _GameListView.VirtualMode = true;
            _GameListView.DrawItem += OnGameListViewDrawItem;
            _GameListView.ItemActivate += OnActivateGame;
            _GameListView.RetrieveVirtualItem += OnGameListViewRetrieveVirtualItem;
            _GameListView.SearchForVirtualItem += OnGameListViewSearchForVirtualItem;
            // 
            // GamePicker
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1633, 640);
            Controls.Add(_GameListView);
            Controls.Add(_PickerStatusStrip);
            Controls.Add(_PickerToolStrip);
            Font = new System.Drawing.Font("新細明體", 11.14286F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 136);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            Name = "GamePicker";
            Text = "Steam Achievement ManagerX 7.0 | Pick a game... Any game...";
            _PickerToolStrip.ResumeLayout(false);
            _PickerToolStrip.PerformLayout();
            _PickerStatusStrip.ResumeLayout(false);
            _PickerStatusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        private MyListView _GameListView;
        private System.Windows.Forms.ImageList _LogoImageList;
        private System.Windows.Forms.Timer _CallbackTimer;
        private System.Windows.Forms.ToolStrip _PickerToolStrip;
        private System.Windows.Forms.ToolStripButton _RefreshGamesButton;
        private System.Windows.Forms.ToolStripTextBox _AddGameTextBox;
        private System.Windows.Forms.ToolStripButton _AddGameButton;
        private System.Windows.Forms.ToolStripDropDownButton _FilterDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem _FilterGamesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterJunkMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterDemosMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterModsMenuItem;
        private System.Windows.Forms.StatusStrip _PickerStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _DownloadStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel _PickerStatusLabel;
        private System.ComponentModel.BackgroundWorker _LogoWorker;
        private System.ComponentModel.BackgroundWorker _ListWorker;
        private System.Windows.Forms.ToolStripTextBox _SearchGameTextBox;
        private System.Windows.Forms.ToolStripLabel _FindGamesLabel;
        private System.Windows.Forms.ToolStripButton _CloseButton;

        #endregion

        private System.Windows.Forms.ToolStripLabel _LanguageLabel;
        private System.Windows.Forms.ToolStripComboBox _LanguageComboBox;
    }
}