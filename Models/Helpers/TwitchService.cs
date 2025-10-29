using Automatic_Replay_Buffer.ViewModel;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    //public static class TwitchHelper
    //{
    //    public static string TokenPath => "token.json";
    //    static string TimeRemaining = "";

    //    private static async Task SaveToken(TokenData token)
    //    {
    //        string json = JsonConvert.SerializeObject(token, Formatting.Indented);
    //        await File.WriteAllTextAsync(TokenPath, json);
    //    }

    //    private static async Task<TokenData?> LoadToken()
    //    {
    //        if (!File.Exists(TokenPath))
    //            return null;

    //        string json = await File.ReadAllTextAsync(TokenPath);
    //        return JsonConvert.DeserializeObject<TokenData>(json);
    //    }

    //    public static async Task<string> GetValidTokenAsync(string clientId, string clientSecret)
    //    {
    //        var token = await LoadToken();

    //        TimeRemaining = $"{token.TimeRemaining.Days}d {token.TimeRemaining.Hours}h {token.TimeRemaining.Minutes}m";

    //        if (token != null && !token.IsExpired)
    //        {
    //            return token.AccessToken;
    //        }

    //        string json = await GetAccessTokenAsync(clientId, clientSecret);

    //        var tokenObj = JsonConvert.DeserializeObject<dynamic>(json);
    //        string accessToken = tokenObj.access_token;
    //        int expiresIn = tokenObj.expires_in;

    //        var newToken = new TokenData
    //        {
    //            AccessToken = accessToken,
    //            ExpiresIn = expiresIn
    //        };

    //        DateTime _ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
    //        TimeSpan _TimeRemaining = _ExpiresAt - DateTime.UtcNow;
    //        TimeRemaining = $"{_TimeRemaining.Days}d {_TimeRemaining.Hours}h {_TimeRemaining.Minutes}m";

    //        await SaveToken(newToken);

    //        return accessToken;
    //    }

    //    public static async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
    //    {
    //        using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

    //        var parameters = new Dictionary<string, string>
    //        {
    //            { "client_id", clientId },
    //            { "client_secret", clientSecret },
    //            { "grant_type", "client_credentials" }
    //        };
    //        var content = new FormUrlEncodedContent(parameters);

    //        try
    //        {
    //            var response = await http.PostAsync("https://id.twitch.tv/oauth2/token", content);

    //            var json = await response.Content.ReadAsStringAsync();

    //            response.EnsureSuccessStatusCode();
    //            return json;
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine($"Error when authenticating with Twitch: {ex.Message}");
    //            throw;
    //        }
    //    }
    //}

    public class TwitchService
    {
        private readonly LoggingService _LoggingService;
        private readonly JsonStorageService StorageService;

        private string path { get; } = "token.json";
        static string TimeRemaining = "";

        public TwitchService(LoggingService logger, JsonStorageService storage)
        {
            _LoggingService = logger;
            StorageService = storage;
        }

        private async Task SaveToken(TokenData token)
        {
            //string json = JsonConvert.SerializeObject(token, Formatting.Indented);
            //await File.WriteAllTextAsync(path, json);
            await JsonStorageService.SaveConfigAsync(path, token);
        }

        private async Task<TokenData?> LoadToken()
        {
            if (!File.Exists(path))
                return null;

            //string json = await File.ReadAllTextAsync(path);
            //return JsonConvert.DeserializeObject<TokenData>(json);
            return await JsonStorageService.LoadConfigAsync(path, new TokenData());
        }

        public async Task<string> GetValidTokenAsync(string clientId, string clientSecret)
        {
            try
            {
                var token = await LoadToken();

                if (token != null && !token.IsExpired)
                {
                    TimeRemaining = $"{token.TimeRemaining.Days}d {token.TimeRemaining.Hours}h {token.TimeRemaining.Minutes}m";
                    return token.AccessToken;
                }

                string json = await GetAccessTokenAsync(clientId, clientSecret);

                var tokenObj = JsonConvert.DeserializeObject<dynamic>(json);
                string accessToken = tokenObj.access_token;
                int expiresIn = tokenObj.expires_in;

                var newToken = new TokenData
                {
                    AccessToken = accessToken,
                    ExpiresIn = expiresIn
                };

                DateTime expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                TimeSpan timeRemaining = expiresAt - DateTime.UtcNow;
                TimeRemaining = $"{timeRemaining.Days}d {timeRemaining.Hours}h {timeRemaining.Minutes}m";

                await SaveToken(newToken);

                _LoggingService.Log("Successfully retrieved new Twitch token");
                Debug.WriteLine("Successfully retrieved new Twitch token");

                return accessToken;
            }
            catch (Exception ex)
            {
                _LoggingService.Log($"Error when getting valid token: {ex.Message}");
                Debug.WriteLine($"Error when getting valid token: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

            var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "client_credentials" }
        };
            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await http.PostAsync("https://id.twitch.tv/oauth2/token", content);
                var json = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                _LoggingService.Log("Successfully requested access token from Twitch");
                Debug.WriteLine("Successfully requested access token from Twitch");
                return json;
            }
            catch (Exception ex)
            {
                _LoggingService.Log($"Error when authenticating with Twitch: {ex.Message}");
                Debug.WriteLine($"Error when authenticating with Twitch: {ex.Message}");
                throw;
            }
        }
    }
}
