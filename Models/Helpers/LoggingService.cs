using System.Diagnostics;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class LoggingService
    {
        public event Action<string>? LogReceived;

        public void Log(string message)
        {
            string timestamp = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogReceived?.Invoke(timestamp);

            Debug.WriteLine(timestamp);
        }
    }
}
