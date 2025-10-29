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
    public class JsonStorageService
    {
        public ClientData Client { get; set; }
        public TokenData Token { get; set; }
        public List<GameData> Game { get; set; }
        public List<FilterData> Filter { get; set; }
        public OBSData OBS { get; set; }

        public static async Task<T> LoadConfigAsync<T>(string path, T obj)
        {
            try
            {
                if (!File.Exists(path))
                {
                    await SaveConfigAsync(path, obj);
                    return obj;
                }

                string json = await File.ReadAllTextAsync(path);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while reading file\"{path}\": {ex.Message}");
                throw;
            }
        }

        public static async Task SaveConfigAsync<T>(string path, T obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while writing to file \"{path}\": {ex.Message}");
            }
        }
    }
}
