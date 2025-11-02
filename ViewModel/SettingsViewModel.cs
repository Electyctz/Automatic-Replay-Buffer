using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public ICommand HomeViewCommand => _parent.HomeViewCommand;
        public ICommand AddFilterCommand => _parent.AddFilterCommand;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
