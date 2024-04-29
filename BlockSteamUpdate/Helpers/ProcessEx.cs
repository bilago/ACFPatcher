using System.Diagnostics;

namespace SteamDisableGameUpdateTool.Helpers
{
    internal class ProcessEx
    {
        public static bool StartProcess(string? filePath)
        {
            try
            {
                if (filePath is null) return false;
                if (!File.Exists(filePath)) return false;
                Process.Start(filePath);
                return true;
            }
            catch
            { return false; }
        }
        public static string? KillProcess(string name)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    var file = p?.MainModule?.FileName;
                    p?.Kill();
                    return file;
                }
                catch (Exception ex)
                {
                    Program.WriteConsole(ex.Message);
                }
            }
            return null;
        }

        public static bool RestartSteam(string? filePath)
        {
            if (filePath != null && File.Exists(filePath))
            {
                Program.WriteConsole("Starting Steam");
                Process.Start(filePath);
                return true;
            }
            else
            {
                var steamLocs = new[] { "C:\\Program Files (x86)\\Steam\\steam.exe", "C:\\Program Files\\Steam\\Steam.exe" };
                foreach (var location in steamLocs)
                {
                    if (File.Exists(location))
                    {
                        Program.WriteConsole("Starting Steam");
                        Process.Start("C:\\Program Files\\Steam\\Steam.exe");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
