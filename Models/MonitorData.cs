using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automatic_Replay_Buffer.Models
{
    public class MonitorData : INotifyPropertyChanged
    {
        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        public string Path { get; set; }
        public string Executable { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override bool Equals(object obj)
            => obj is MonitorData m && m.Title == Title && m.Executable == Executable && m.Path == Path;

        public override int GetHashCode() => (Title + Executable + Path).GetHashCode();
    }
}
