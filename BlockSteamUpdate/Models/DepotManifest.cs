namespace SteamDisableGameUpdateTool.Models
{
    public class DepotManifest
    {
        public string? DepotId { get; set; }
        public string? ManifestId { get; set; }
        public bool Mandatory { get; set; }
        public string? Language { get; set; }
        //public string? DlcName { get; set; }
        public bool IsLanguage => !string.IsNullOrEmpty(Language);
        //public bool IsDlc => !string.IsNullOrEmpty(DlcName);
    }
}
