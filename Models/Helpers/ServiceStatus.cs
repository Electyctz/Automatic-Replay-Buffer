using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public enum ServiceState
    {
        Offline,
        Busy,
        Online
    }

    class ServiceStatus
    {
        public static System.Windows.Media.Brush GetBrush(ServiceState state)
        {
            SolidColorBrush brush = state switch
            {
                ServiceState.Online => new SolidColorBrush(Colors.LimeGreen),
                ServiceState.Busy => new SolidColorBrush(Colors.DarkOrange),
                _ => new SolidColorBrush(Colors.Red)
            };
            brush.Freeze();
            return brush;
        }

        public static string GetText(string serviceName, ServiceState state) => (serviceName, state) switch
        {
            ("Websocket", ServiceState.Online) => "Connected",
            ("Websocket", ServiceState.Busy) => "Connecting",
            ("Websocket", _) => "Disconnected",

            ("Monitor", ServiceState.Online) => "Running",
            ("Monitor", ServiceState.Busy) => "Starting",
            ("Monitor", _) => "Stopped",

            ("Database", ServiceState.Online) => "Available",
            ("Database", ServiceState.Busy) => "Loading",
            ("Database", _) => "Unavailable",

            _ => "Unknown"
        };
    }
}
