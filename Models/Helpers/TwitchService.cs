using Automatic_Replay_Buffer.ViewModel;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class TwitchService(LoggingService LoggingService, JsonStorageService StorageService)
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private string TokenPath { get; } = "token.json";

        private async Task SaveTokenAsync(TokenData token)
        {
            try
            {
                await StorageService.SaveConfigAsync(TokenPath, token);
            }
            catch { }
            finally
            {
                LoggingService.Log("Twitch token saved");
            }
        }

        private async Task<TokenData?> LoadTokenAsync()
        {
            if (!File.Exists(TokenPath))
                return null;

            return await StorageService.LoadConfigAsync(TokenPath, new TokenData());
        }

        public async Task<bool> AuthenticateTokenAsync()
        {
            var token = await LoadTokenAsync();

            if (!string.IsNullOrWhiteSpace(token.AccessToken))
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
                    request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", token.AccessToken);

                    using var response = await HttpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    LoggingService.Log($"Error when authenticating token: {ex.Message}");
                }
            }

            return false;
        }

        public async Task<string> GetTwitchTokenAsync(string clientId, string clientSecret)
        {
            var parameters = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" }
            };
            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await HttpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);
                var json = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                string accessToken = obj.access_token;

                var token = new TokenData
                {
                    AccessToken = accessToken
                };

                await SaveTokenAsync(token);

                return token.AccessToken;
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Error when authenticating with Twitch: {ex.Message}");
                throw;
            }
        }
    }
}
