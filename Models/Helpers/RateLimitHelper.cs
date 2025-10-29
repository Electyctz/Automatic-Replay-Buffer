using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class RateLimitHelper
    {
        private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(4, 4);

        public static async Task<HttpResponseMessage> SendWithRateLimit(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                return await client.SendAsync(request, cancellationToken);
            }
            finally
            {
                _ = Task.Delay(250, cancellationToken).ContinueWith(_ => _rateLimiter.Release());
            }
        }
    }
}
