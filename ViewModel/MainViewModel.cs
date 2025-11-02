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

        public ObservableCollection<MonitorData> ActiveGames { get; private set; } = [];

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public ICommand HomeViewCommand { get; }
        public ICommand SettingsViewCommand { get; }
        public ICommand AddFilterCommand { get; }

        public HomeViewModel HomeVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }


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

        public Brush StatusBrush =>
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

        private string _filterTitle;
        public string FilterTitle
        {
            get => _filterTitle;
            set { _filterTitle = value; OnPropertyChanged(); }
        }
        private string _filterPath;
        public string FilterPath
        {
            get => _filterPath;
            set { _filterPath = value; OnPropertyChanged(); }
        }
        private string _filterExecutable;
        public string FilterExecutable
        {
            get => _filterExecutable;
            set { _filterExecutable = value; OnPropertyChanged(); }
        }
        #endregion

        public MainViewModel()
        {
            HomeVM = new HomeViewModel(this);
            SettingsVM = new SettingsViewModel(this);
            CurrentView = HomeVM;

            StorageService = new JsonStorageService(LoggingService);
            OBSService = new OBSService(LoggingService, this);

            LoggingService.LogReceived += OnLogReceived;

            HomeViewCommand = new RelayCommand(obj =>
            {
                CurrentView = HomeVM;
            });

            SettingsViewCommand = new RelayCommand(obj =>
            {
                CurrentView = SettingsVM;
            });

            AddFilterCommand = new RelayCommand(async obj => await AddFilterAsync(obj));
        }

        public async Task InitializeAsync()
        {
            await StorageService.LoadAsync();

            LoggingService.Log("Starting game monitoring service...");
            MonitorState = ServiceState.Busy;

            LoggingService.Log("Loading game database...");
            DatabaseState = ServiceState.Busy;

            StorageService.Game = await DownloadDatabaseAsync();
            DatabaseState = (StorageService.Game?.Count ?? 0) > 0 ? ServiceState.Online : ServiceState.Offline;
            LoggingService.Log($"Loaded game database with {StorageService.Game?.Count ?? 0} entries");
            await StartMonitoringAsync();
        }

        private async Task AddFilterAsync(object obj)
        {
            try
            {
                bool changed = false;

                if (obj is IList selectedItems)
                {
                    foreach (var item in selectedItems.Cast<MonitorData>())
                    {
                        if (Utilities.FilterExists(StorageService.Filter, item.Title, item.Path, item.Executable))
                            changed = true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(FilterTitle) ||
                         !string.IsNullOrWhiteSpace(FilterPath) ||
                         !string.IsNullOrWhiteSpace(FilterExecutable))
                {
                    if (Utilities.FilterExists(StorageService.Filter, FilterTitle, FilterPath, FilterExecutable))
                    {
                        changed = true;
                        FilterTitle = FilterPath = FilterExecutable = string.Empty;
                    }
                }

                if (!changed)
                    return;

                await StorageService.SaveAsync("settings.json");

                var toRemove = ActiveGames
                    .Where(g => StorageService.Filter.Any(f =>
                        (!string.IsNullOrEmpty(f.Title) && f.Title.Equals(g.Title, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(f.Path) && f.Path.Equals(g.Path, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(f.Executable) && f.Executable.Equals(g.Executable, StringComparison.OrdinalIgnoreCase))))
                    .ToList();

                foreach (var game in toRemove)
                    ActiveGames.Remove(game);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Failed to add filter: {ex.Message}");
            }
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
