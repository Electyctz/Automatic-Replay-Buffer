using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models
{
    public class AppSettings
    {
        public List<FilterData> Filter { get; set; } = new();
        public OBSData OBS { get; set; } = new();
    }
}
