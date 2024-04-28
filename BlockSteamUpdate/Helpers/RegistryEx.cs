using Microsoft.Win32;
using System.Runtime.Versioning;

namespace SteamDisableGameUpdateTool.Helpers
{
    internal class RegistryEx
    {
        [SupportedOSPlatform("windows")]
        public static string? GetInstallDir(string registryKeyPath)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(registryKeyPath);
                if (key != null)                
                    return key.GetValue("installed path")?.ToString() ?? key.GetValue("InstallLocation")?.ToString();
            }
            catch (Exception ex)
            {
                Program.WriteConsole($"Error reading registry: {ex.Message}");
            }
            return null;
        }
    }
}
