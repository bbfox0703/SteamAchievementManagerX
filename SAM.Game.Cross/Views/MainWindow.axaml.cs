using System.Collections.ObjectModel;
using Avalonia.Controls;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Avalonia.Markup.Xaml;
using SAM.Game.Core;
using SAM.Game.Stats;

namespace SAM.Game.Cross.Views;

public partial class MainWindow : Window
{
    private readonly GameService _service;
    public ObservableCollection<AchievementInfo> Achievements { get; } = new();
    public ObservableCollection<StatInfo> Stats { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public MainWindow(GameService service) : this()
    {
        _service = service;
        LoadData();
    }

    private async void LoadData()
    {
        if (!_service.RequestCurrentStats())
        {
            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Error",
                ContentMessage = "Failed to request stats.",
                ButtonDefinitions = ButtonEnum.Ok
            }).ShowAsync(this);
            return;
        }

        Achievements.Clear();
        foreach (var a in _service.GetAchievements())
            Achievements.Add(a);

        Stats.Clear();
        foreach (var s in _service.GetStats())
            Stats.Add(s);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
