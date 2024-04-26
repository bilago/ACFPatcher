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
        public Dictionary<string, string> DepotManifests { get; set; } = new();
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
