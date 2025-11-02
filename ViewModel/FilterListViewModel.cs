using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Automatic_Replay_Buffer.ViewModel
{
    public class FilterListViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _parent;

        public FilterListViewModel(MainViewModel parent)
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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
