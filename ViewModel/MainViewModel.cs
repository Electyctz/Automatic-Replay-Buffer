using Automatic_Replay_Buffer.Models;
using Automatic_Replay_Buffer.Models.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace Automatic_Replay_Buffer.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region
        private CancellationTokenSource? ctsFetch;
        private CancellationTokenSource? ctsMonitor;
        public JsonStorageService StorageService;
        public GameMonitorService MonitorService;
        public OBSService OBSService { get; }
        public OBSData OBSData => StorageService.OBS;
        public LoggingService LoggingService { get; } = new();
        public TwitchService TwitchService;

        public ObservableCollection<MonitorData> ActiveGames { get; private set; } = [];

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public ICommand FetchDatabaseCommand { get; }
        public ICommand CancelFetchCommand { get; }
        public ICommand AddFilterCommand { get; }

        private StringBuilder _logText = new();
        public string LogText
        {
            get => _logText.ToString();
            private set
            {
                _logText = new StringBuilder(value);
                OnPropertyChanged();
            }
        }

        //private string _databaseProgressText;
        //public string DatabaseProgressText
        //{
        //    get => _databaseProgressText;
        //    set
        //    {
        //        if (_databaseProgressText != value)
        //        {
        //            _databaseProgressText = value;
        //            OnPropertyChanged(nameof(DatabaseProgressText));
        //        }
        //    }
        //}

        //private int _databaseProgressValue;
        //public int DatabaseProgressValue
        //{
        //    get => _databaseProgressValue;
        //    set
        //    {
        //        if (_databaseProgressValue != value)
        //        {
        //            _databaseProgressValue = value;
        //            OnPropertyChanged(nameof(DatabaseProgressValue));
        //        }
        //    }
        //}

        public System.Windows.Media.Brush StatusBrush =>
        StatusText.Equals("Idle", StringComparison.OrdinalIgnoreCase)
        ? new SolidColorBrush(Colors.DarkOrange)
        : new SolidColorBrush(Colors.Green);
        private string _statusText = "Idle";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusBrush));
                }
            }
        }

        private ServiceState _websocketState = ServiceState.Offline;
        public ServiceState WebsocketState
        {
            get => _websocketState;
            set
            {
                if (_websocketState != value)
                {
                    _websocketState = value;
                    OnPropertyChanged(nameof(WebsocketText));
                    OnPropertyChanged(nameof(WebsocketBrush));
                }
            }
        }
        public string WebsocketText => ServiceStatus.GetText("Websocket", WebsocketState);
        public Brush WebsocketBrush => ServiceStatus.GetBrush(WebsocketState);

        private ServiceState _monitorState = ServiceState.Offline;
        public ServiceState MonitorState
        {
            get => _monitorState;
            set
            {
                if (_monitorState != value)
                {
                    _monitorState = value;
                    OnPropertyChanged(nameof(MonitorText));
                    OnPropertyChanged(nameof(MonitorBrush));
                }
            }
        }
        public string MonitorText => ServiceStatus.GetText("Monitor", MonitorState);
        public Brush MonitorBrush => ServiceStatus.GetBrush(MonitorState);

        private ServiceState _databaseState = ServiceState.Offline;
        public ServiceState DatabaseState
        {
            get => _databaseState;
            set
            {
                if (_databaseState != value)
                {
                    _databaseState = value;
                    OnPropertyChanged(nameof(DatabaseText));
                    OnPropertyChanged(nameof(DatabaseBrush));
                }
            }
        }
        public string DatabaseText => ServiceStatus.GetText("Database", DatabaseState);
        public Brush DatabaseBrush => ServiceStatus.GetBrush(DatabaseState);
        #endregion

        public MainViewModel()
        {
            StorageService = new JsonStorageService(LoggingService);
            OBSService = new OBSService(LoggingService, this);
            TwitchService = new TwitchService(LoggingService, StorageService);

            LoggingService.LogReceived += OnLogReceived;

            //FetchDatabaseCommand = new RelayCommand(async _ =>
            //{
            //    ctsFetch = new CancellationTokenSource();
            //    try
            //    {
            //        IsFetching = true;
            //        //await FetchDatabaseAsync(ctsFetch.Token);
            //    }
            //    catch (OperationCanceledException)
            //    {
            //        LoggingService.Log("Database operation was cancelled");
            //    }
            //    finally
            //    {
            //        IsFetching = false;
            //        ctsFetch.Dispose();
            //        ctsFetch = null;

            //        if (ctsFetch == null)
            //        {
            //            StatusText = "Idle";
            //            DatabaseText = (StorageService.Game?.Count ?? 0) > 0 ? "Available" : "Not Found";
            //            DatabaseProgressValue = 0;
            //            DatabaseProgressText = "";
            //        }
            //     }
            //});

            //CancelFetchCommand = new RelayCommand(_ => ctsFetch?.Cancel());

            AddFilterCommand = new RelayCommand(async obj =>
            {
                if (obj is not IList selectedItems) return;

                try
                {
                    var existingFilter = await StorageService.LoadConfigAsync("filter.json", new List<FilterData>());
                    bool changed = false;

                    foreach (var item in selectedItems.Cast<MonitorData>())
                    {
                        bool exists = existingFilter.Any(f =>
                            (!string.IsNullOrEmpty(f.Title) && f.Title.Equals(item.Title, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(f.Path) && f.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(f.Executable) && f.Executable.Equals(item.Executable, StringComparison.OrdinalIgnoreCase))
                        );

                        if (!exists)
                        {
                            existingFilter.Add(new FilterData
                            {
                                Title = item.Title,
                                Path = item.Path,
                                Executable = item.Executable
                            });
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        StorageService.Filter = existingFilter;
                        await StorageService.SaveConfigAsync("filter.json", existingFilter);

                        foreach (var item in selectedItems.Cast<MonitorData>().ToList())
                        {
                            bool isFiltered = StorageService.Filter.Any(f =>
                                (!string.IsNullOrEmpty(f.Title) && f.Title.Equals(item.Title, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Path) && f.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Executable) && f.Executable.Equals(item.Executable, StringComparison.OrdinalIgnoreCase))
                            );

                            if (isFiltered)
                                ActiveGames.Remove(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Log($"Failed to add selected items to filter: {ex.Message}");
                }
            });
        }

        public async Task InitializeAsync()
        {
            StorageService.Filter = await StorageService.LoadConfigAsync("filter.json", new List<FilterData>());
            StorageService.OBS = await StorageService.LoadConfigAsync("websocket.json", new OBSData { Address = "", Password = "" });

            OBSService.Connect(OBSData.Address, OBSData.Password);

            LoggingService.Log("Starting game monitoring service...");
            MonitorState = ServiceState.Busy;

            LoggingService.Log("Loading game database...");
            DatabaseState = ServiceState.Busy;

            StorageService.Game = await DownloadDatabaseAsync();
            DatabaseState = (StorageService.Game?.Count ?? 0) > 0 ? ServiceState.Online : ServiceState.Offline;
            LoggingService.Log($"Loaded game database with {StorageService.Game?.Count ?? 0} entries");
            await StartMonitoringAsync();
        }

        public async Task<List<GameData>> DownloadDatabaseAsync()
        {
            try
            {
                string json = await HttpClient.GetStringAsync("https://drive.usercontent.google.com/download?id=1EAtAtC4ln2EJxg91c34Zo3-sJoNzWuhP");
                List<GameData> database = await Task.Run(() => JsonConvert.DeserializeObject<List<GameData>>(json));

                return database;
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Error loading game database: {ex.Message}");
                return [];
            }
        }

        public async Task StartMonitoringAsync()
        {
            MonitorService = new GameMonitorService(LoggingService, StorageService, OBSService, this, true);

            ctsMonitor = new CancellationTokenSource();

            var statusProgress = new Progress<string>(s =>
            {
                StatusText = s;
            });

            var gamesProgress = new Progress<List<MonitorData>>(newGames =>
            {
                var existing = ActiveGames.ToDictionary(g => g.Executable);
                var incoming = newGames.ToDictionary(g => g.Executable);

                // remove games that are no longer running
                foreach (var g in ActiveGames.ToList())
                {
                    if (!incoming.ContainsKey(g.Executable))
                        ActiveGames.Remove(g);
                }

                // add new games that aren't in the list
                foreach (var g in newGames)
                {
                    if (!existing.ContainsKey(g.Executable))
                        ActiveGames.Add(g);
                }

                // update properties if something changed (title)
                foreach (var g in ActiveGames)
                {
                    if (incoming.TryGetValue(g.Executable, out var updated))
                    {
                        if (g.Title != updated.Title)
                            g.Title = updated.Title;
                    }
                }
            });

            await MonitorService.MonitorGamesAsync(statusProgress, gamesProgress, ctsMonitor.Token);
        }

        //private async Task FetchDatabaseAsync(CancellationToken cts)
        //{
        //    TwitchService = new TwitchService(LoggingService, StorageService);

        //    string token;

        //    if (await TwitchService.AuthenticateTokenAsync())
        //    {
        //        token = StorageService.Token.AccessToken;
        //    }
        //    else
        //    {
        //        token = await TwitchService.GetTwitchTokenAsync(StorageService.Client.ID, StorageService.Client.Secret);
        //    }

        //    try
        //    {
        //        if (!string.IsNullOrWhiteSpace(token))
        //        {
        //            const int pageSize = 500;
        //            int offset = 0;
        //            int totalEstimated = 341_215;
        //            int totalFetched = 0;

        //            DatabaseProgressValue = 0;
        //            DatabaseText = "Fetching";
        //            StatusText = "Fetching Database";
        //            LoggingService.Log("Fetching database...");

        //            using var http = new HttpClient();
        //            http.DefaultRequestHeaders.Add("Client-ID", StorageService.Client.ID);
        //            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        //            var allGames = new List<GameData>();

        //            while (true)
        //            {
        //                cts.ThrowIfCancellationRequested();

        //                string query = $@"
        //            fields id, name, game_type;
        //            where (game_type = 0 | game_type = 1 | game_type = 8 | game_type = 9 | game_type = 10 | game_type = 11);
        //            limit {pageSize};
        //            offset {offset};
        //            sort id asc;";

        //                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games")
        //                {
        //                    Content = new StringContent(query, Encoding.UTF8, "text/plain")
        //                };

        //                var response = await RateLimitHelper.SendWithRateLimit(http, request, cts);

        //                if (!response.IsSuccessStatusCode)
        //                    break;

        //                var json = await response.Content.ReadAsStringAsync(cts);
        //                var games = JsonConvert.DeserializeObject<List<GameData>>(json);

        //                if (games == null || games.Count == 0)
        //                    break;

        //                allGames.AddRange(games);
        //                offset += pageSize;
        //                totalFetched += games.Count;

        //                DatabaseProgressValue = Math.Min(100, (int)((double)totalFetched / totalEstimated * 100));
        //                DatabaseProgressText = $"Fetched {totalFetched}/{totalEstimated} (estimated) games...";
        //            }

        //            if (allGames.Count == 0)
        //            {
        //                LoggingService.Log("No games were fetched from the database");
        //                return;
        //            }

        //            await StorageService.SaveConfigAsync("games.json", allGames);

        //            StorageService.Game = allGames;

        //            DatabaseProgressValue = 100;
        //            DatabaseProgressText = $"Database fetched! {allGames.Count} games saved.";
        //            DatabaseText = "Available";
        //            StatusText = "Idle";
        //            LoggingService.Log("Finished fetching database");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggingService.Log($"Error while fetching database: {ex.Message}");
        //        throw;
        //    }
        //}

        private void OnLogReceived(string msg)
        {
            _logText.AppendLine(msg);
            OnPropertyChanged(nameof(LogText));
        }

        public void SleepyTime()
        {
            ctsMonitor?.Cancel();
            ctsFetch?.Cancel();
        }
    }
}
