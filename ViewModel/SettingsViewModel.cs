using System.ComponentModel;
using System.Windows.Input;

namespace Automatic_Replay_Buffer.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _parent;

        public SettingsViewModel(MainViewModel parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            if (_parent is INotifyPropertyChanged npc)
                npc.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e?.PropertyName));
        }

        public ICommand HomeViewCommand => _parent.HomeViewCommand;
        public ICommand AddWhitelistCommand => _parent.AddWhitelistCommand;
        public ICommand AddBlacklistCommand => _parent.AddBlacklistCommand;
        public ICommand FilterListViewCommand => _parent.FilterListViewCommand;

        public string FilterTitle
        {
            get => _parent.FilterTitle;
            set
            {
                _parent.FilterTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterTitle)));
            }
        }
        public string FilterPath
        {
            get => _parent.FilterPath;
            set
            {
                _parent.FilterPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterPath)));
            }
        }
        public string FilterExecutable
        {
            get => _parent.FilterExecutable;
            set
            {
                _parent.FilterExecutable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterExecutable)));
            }
        }
        public string OBSAddress
        {
            get => _parent.OBSAddress;
            set
            {
                _parent.OBSAddress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OBSAddress)));
            }
        }
        public string OBSPassword
        {
            get => _parent.OBSPassword;
            set
            {
                _parent.OBSPassword = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OBSPassword)));
            }
        }
        public bool StartWithWindows
        {
            get => _parent.StartWithWindows;
            set
            {
                _parent.StartWithWindows = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartWithWindows)));
            }
        }
        public bool MinimizeToTray
        {
            get => _parent.MinimizeToTray;
            set
            {
                _parent.MinimizeToTray = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimizeToTray)));
            }
        }
        public bool StartMinimized
        {
            get => _parent.StartMinimized;
            set
            {
                _parent.StartMinimized = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartMinimized)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
