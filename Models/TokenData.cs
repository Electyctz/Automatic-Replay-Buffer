using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models
{
    public class TokenData
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }

        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt => FetchedAt.AddSeconds(ExpiresIn);
        public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;

        public bool IsExpired => TimeRemaining.TotalMinutes <= 5;
    }
}
