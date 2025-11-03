using System.Collections.ObjectModel;

namespace Automatic_Replay_Buffer.Models
{
    public class AppSettings
    {
        public ObservableCollection<FilterData> Filter { get; set; } = new();
        public OBSData OBS { get; set; } = new();
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
    }
}