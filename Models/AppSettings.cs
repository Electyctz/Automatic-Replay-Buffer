using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models
{
    public class AppSettings
    {
        public ObservableCollection<FilterData> Filter { get; set; } = new();
        public OBSData OBS { get; set; } = new();
    }
}
