namespace Automatic_Replay_Buffer.Models
{
    public class MonitorData : ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Executable { get; set; } = string.Empty;
    }
}
