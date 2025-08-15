using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SAM.Picker.Modern
{
    public class GameViewModel : INotifyPropertyChanged
    {
        public uint Id { get; }
        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private BitmapImage? _logo;
        public BitmapImage? Logo { get => _logo; set { _logo = value; OnPropertyChanged(); } }

        public GameViewModel(uint id, string name)
        {
            Id = id;
            _name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
