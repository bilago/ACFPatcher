using Microsoft.Win32;
using System.Diagnostics;

class Program
{
    static Dictionary<string, string> ReplaceDict()
    {
        return new Dictionary<string, string>()
        {
            {"\"buildid\"\t\t\"4460038\"", "\"buildid\"\t\t\"14160910\"" },
            {"\"AutoUpdateBehavior\"\t\t\"0\"", "\"AutoUpdateBehavior\"\t\t\"1\"" },
            {"\"ScheduledAutoUpdate\"\t\t\"1\"","\"ScheduledAutoUpdate\"\t\t\"0\"" },
            {"\"manifest\"\t\t\"7497069378349273908\"","\"manifest\"\t\t\"7332110922360867314\"" },
            {"\"manifest\"\t\t\"5847529232406005096\"","\"manifest\"\t\t\"3747866029627240371\"" },
            {"\"manifest\"\t\t\"5819088023757897745\"","\"manifest\"\t\t\"3876298980394425306\"" },
            {"\"manifest\"\t\t\"2178106366609958945\"","\"manifest\"\t\t\"8492427313392140315\"" },
            {"\"manifest\"\t\t\"1691678129192680960\"","\"manifest\"\t\t\"1213339795579796878\"" },
            {"\"manifest\"\t\t\"5106118861901111234\"","\"manifest\"\t\t\"7785009542965564688\"" },
            {"\"manifest\"\t\t\"1255562923187931216\"","\"manifest\"\t\t\"366079256218893805\"" }
        };
    }

    public static void SetReadOnly(string file)
    {
        try
        {
            if (!File.Exists(file))
                return;
            var finfo = new FileInfo(file);
            if (finfo.IsReadOnly)
                return;

            var attr = File.GetAttributes(file);

            // set read-only
            attr = attr | FileAttributes.ReadOnly;
            File.SetAttributes(file, attr);
            Console.WriteLine($"Set {file} to read only");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static void RemoveReadOnly(string file)
    {
        try
        {
            if (!File.Exists(file))
                return;

            var finfo = new FileInfo(file);
            if (!finfo.IsReadOnly)
                return;

            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
            Console.WriteLine($"Removed Read-Only attribute from {file}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    private static string GetManifestDirectory()
    {
        try
        {
            var fileName = "appmanifest_377160.acf";
            if (File.Exists("Fallout4.exe") && Environment.CurrentDirectory.Contains("common\\Fallout 4"))
            {
                Console.WriteLine($"File was ran from the game directory, using {Environment.CurrentDirectory}");
                var fullPath =  Path.Combine(Environment.CurrentDirectory.Replace("common\\Fallout 4", ""), fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            // Registry key paths for Fallout 4 installation
            string[] registryKeyPaths = new string[]
            {
            @"SOFTWARE\WOW6432Node\Bethesda Softworks\Fallout4",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 377160"
            };

            string installDir = null;

            foreach (var registryKeyPath in registryKeyPaths)
            {
                installDir = GetInstallDirFromRegistry(registryKeyPath);
                if (!string.IsNullOrEmpty(installDir))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(installDir))
            {
                Console.WriteLine("Fallout 4 installation directory not found in registry.");
                return null;
            }

            // Construct the path to the Steam manifest file
            string steamManifestPath = Path.Combine(installDir.Replace("common\\Fallout 4", ""), fileName);

            if (!File.Exists(steamManifestPath))
            {
                Console.WriteLine("Steam manifest file not found.");
                return null;
            }
            return steamManifestPath;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }
    static void Main(string[] args)
    {
        try
        {
            var steamManifestPath = GetManifestDirectory() ?? throw new Exception("Could not locate the manifest file on your machine. Place this exe in your fallout 4 directory for detection");            
            var backupFile = $"{steamManifestPath}.backup_{DateTime.Now.ToString("ddmmyyyyhhMMss")}";

            Console.WriteLine($"Manifest Location: {steamManifestPath}");
            Console.WriteLine($"Backup Location: {backupFile}");
            Console.WriteLine("Press Y to start or any other key to abort.");
            var key = Console.ReadKey();
            Console.WriteLine();
            if(key.Key != ConsoleKey.Y)
            {
                Console.WriteLine($"Execution aborted, press any key to exit");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Shutting down steam");
            var steamFilePath = KillProcess("Steam");

            File.Copy(steamManifestPath, backupFile, true);
            Console.WriteLine($"Manifest file backed up to {backupFile}");

            // Read the manifest file content
            var manifestContent = File.ReadAllText(steamManifestPath);
            foreach (var item in ReplaceDict())
            {
                if (manifestContent.Contains(item.Key))
                {
                    manifestContent = manifestContent.Replace(item.Key, item.Value);
                    Console.WriteLine($"Replaced {item.Key} with {item.Value}");
                }
            }
            RemoveReadOnly(steamManifestPath);
            File.Delete(steamManifestPath);
            File.WriteAllText(steamManifestPath, manifestContent);
            SetReadOnly(steamManifestPath);

            var started = false;
            if (steamFilePath != null && File.Exists(steamFilePath))
            {
                
                Process.Start(steamFilePath);
                started = true;
            }
            else
            {
                if (File.Exists("C:\\Program Files\\Steam\\Steam.exe"))
                {
                    Process.Start("C:\\Program Files\\Steam\\Steam.exe");
                    started = true;
                }
            }
            Console.WriteLine($"Manifest file updated successfully. {(started ? "Steam restarted" : "Could not start steam, please launch manually")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Console.WriteLine("Finished. Press any key to exit");
        Console.ReadKey();
    }

    private static string? KillProcess(string name)
    {
        foreach (var p in Process.GetProcessesByName(name))
        {
            try
            {
                var file = p?.MainModule?.FileName;
                p?.Kill();
                return file;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return null;
    }

    static string? GetInstallDirFromRegistry(string registryKeyPath)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(registryKeyPath))
            {
                if (key != null)
                {
                    return key.GetValue("installed path")?.ToString() ?? key.GetValue("InstallLocation")?.ToString();                    
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading registry: {ex.Message}");
        }
        return null;
    }
}
