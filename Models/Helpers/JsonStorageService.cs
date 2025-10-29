using Automatic_Replay_Buffer.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class JsonStorageService(LoggingService _loggingService)
    {
        private readonly LoggingService LoggingService = _loggingService;
        public ClientData Client { get; set; } = new();
        public TokenData Token { get; set; } = new();
        public List<GameData> Game { get; set; } = new();
        public List<FilterData> Filter { get; set; } = new();
        public OBSData OBS { get; set; } = new();

        public async Task<T> LoadConfigAsync<T>(string _path, T _obj)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    await SaveConfigAsync(_path, _obj);
                    return _obj;
                }

                string json = await File.ReadAllTextAsync(_path);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Error while reading file \"{_path}\": {ex.Message}");
                throw;
            }
        }

        public async Task SaveConfigAsync<T>(string _path, T _obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(_obj, Formatting.Indented);
                await File.WriteAllTextAsync(_path, json);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Error while writing to file \"{_path}\": {ex.Message}");
            }
        }
    }
}
