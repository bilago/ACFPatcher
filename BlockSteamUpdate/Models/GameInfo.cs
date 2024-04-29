using SteamDisableGameUpdateTool.Models;
using System.Text.Json;

namespace SteamSkipNextGenUpdate.Models
{
    public class GameInfo
    {
        public string? Name { get; set; }
        public string? Binary { get; set; }
        public string? AppId { get; set; }
        public List<string> RegistryLocations { get; set; } = new();
        public Dictionary<string, string> Main { get; set; } = new();
        public List<DepotManifest> DepotManifests { get; set; } = new();
        //public List<string> MandatoryDepots { get; set; } = new();
        //public Dictionary<string, string> LanguageSpecificDepots { get; set; } = new();
        public Dictionary<string, List<string>> DLCDepots { get; set; } = new();
        public string? GetDlcName(string depotId)
        {
            var dlc = DLCDepots?.FirstOrDefault(x=>x.Value.Contains(depotId));
            if (!dlc.HasValue)
                return null;
            return dlc.Value.Key;
        }
        public static GameInfo? FromFile(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                    return null;
                var content = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<GameInfo>(content);
            }
            catch
            {
                return null;
            }
        }
    }
}
