using SAM.Picker.Core;
using API = SAM.API;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace SAM.Picker.Modern
{
    public class MainViewModel
    {
        private readonly API.Client _client;
        private readonly HttpClient _httpClient;
        private readonly IconCache _iconCache;

        public ObservableCollection<GameViewModel> Games { get; } = new();
        public ObservableCollection<GameViewModel> FilteredGames { get; } = new();

        public MainViewModel()
        {
            _client = new API.Client();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _iconCache = new IconCache(_httpClient, Path.Combine(AppContext.BaseDirectory, "appcache"));
        }

        public async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                bool usedLocal;
                var bytes = GameList.Load(AppContext.BaseDirectory, _httpClient, out usedLocal);
                using var stream = new MemoryStream(bytes, false);
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using var reader = XmlReader.Create(stream, settings);
                var document = new XPathDocument(reader);
                var navigator = document.CreateNavigator();
                var nodes = navigator.Select("/games/game");
                while (nodes.MoveNext())
                {
                    string type = nodes.Current.GetAttribute("type", "");
                    if (string.IsNullOrEmpty(type)) { type = "normal"; }
                    uint id = (uint)nodes.Current.ValueAsLong;
                    if (_client.SteamApps008.IsSubscribedApp(id))
                    {
                        var name = _client.SteamApps001.GetAppData(id, "name");
                        var game = new GameViewModel(id, name);
                        Games.Add(game);
                    }
                }
            });

            foreach (var game in Games)
            {
                AddFiltered(game);
                _ = LoadLogoAsync(game);
            }
        }

        private void AddFiltered(GameViewModel game)
        {
            FilteredGames.Add(game);
        }

        public void Filter(string text)
        {
            FilteredGames.Clear();
            foreach (var game in Games.Where(g => string.IsNullOrEmpty(text) || g.Name.Contains(text, StringComparison.OrdinalIgnoreCase)))
            {
                FilteredGames.Add(game);
            }
        }

        private async Task LoadLogoAsync(GameViewModel game)
        {
            var url = _client.SteamApps001.GetAppData(game.Id, "logo");
            if (string.IsNullOrEmpty(url)) return;
            if (!ImageUrlValidator.TryCreateUri(url, out var uri) ) return;
            try
            {
                var data = await _iconCache.GetOrDownloadAsync(game.Id, uri);
                if (data == null) return;
                using InMemoryRandomAccessStream mem = new();
                await mem.WriteAsync(data.AsBuffer());
                mem.Seek(0);
                BitmapImage bmp = new();
                await bmp.SetSourceAsync(mem);
                game.Logo = bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Launch(GameViewModel game)
        {
            try
            {
                var exe = Path.Combine(AppContext.BaseDirectory, "SAM.Game.exe");
                Process.Start(new ProcessStartInfo(exe, game.Id.ToString()) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
