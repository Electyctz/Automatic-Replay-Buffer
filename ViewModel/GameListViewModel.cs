using Automatic_Replay_Buffer.Models;
using System.ComponentModel;
using System.Windows.Input;

namespace Automatic_Replay_Buffer.ViewModel
{
    public class GameListViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _parent;

        public GameListViewModel(MainViewModel parent)
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
        public ICommand AddFilterCommand => _parent.AddFilterCommand;
        public IEnumerable<MonitorData> ActiveGames => _parent.ActiveGames;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
