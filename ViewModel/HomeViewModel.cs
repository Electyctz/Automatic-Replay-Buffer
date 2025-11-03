using System.ComponentModel;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;

namespace Automatic_Replay_Buffer.ViewModel
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _parent;

        public HomeViewModel(MainViewModel parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            if (_parent is INotifyPropertyChanged npc)
                npc.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e?.PropertyName));
        }

        public ICommand SettingsViewCommand => _parent.SettingsViewCommand;
        public ICommand GameListViewCommand => _parent.GameListViewCommand;

        public string WebsocketText => _parent.WebsocketText;
        public Brush WebsocketBrush => _parent.WebsocketBrush;
        public string MonitorText => _parent.MonitorText;
        public Brush MonitorBrush => _parent.MonitorBrush;
        public string DatabaseText => _parent.DatabaseText;
        public Brush DatabaseBrush => _parent.DatabaseBrush;
        public string StatusText => _parent.StatusText;
        public Brush StatusBrush => _parent.StatusBrush;
        public string LogText => _parent.LogText;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
