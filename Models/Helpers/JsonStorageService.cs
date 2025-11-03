using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class JsonStorageService
    {
        private readonly LoggingService LoggingService;
        public AppSettings Settings { get; private set; } = new();
        public ObservableCollection<FilterData> Filter => Settings.Filter ??= new ObservableCollection<FilterData>();
        public OBSData OBS => Settings.OBS;
        public List<GameData> Game { get; set; } = new();

        public JsonStorageService(LoggingService _LoggingService)
        {
            LoggingService = _LoggingService;
        }

        public async Task LoadAsync(string path = "settings.json")
        {
            if (!File.Exists(path))
            {
                await SaveAsync(path);
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(path);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Failed to load settings: {ex.Message}");
                Settings = new AppSettings();
            }
        }

        public async Task SaveAsync(string path = "settings.json")
        {
            try
            {
                var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
