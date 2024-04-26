using Microsoft.Win32;
using SteamSkipNextGenUpdate.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var gameDict = new Dictionary<string, GameInfo>();
            var gameList = new List<string>();
            GameInfo? selectedGame = null;
            foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*_GameInfo.json"))
            {
                try
                {
                    var game = GameInfo.FromFile(file);
                    if (game?.Name is null) continue;
                    gameDict[game.Name] = game;
                    gameList.Add(game.Name);
                    Console.WriteLine("\n\n");
                    Console.WriteLine("ACF File Patcher - By Bilago");
                    Console.WriteLine("Choose a game to prevent steam updates:");
                    var place = 1;
                    foreach (var g in gameList)
                    {
                        Console.WriteLine($"{place++}. {g}");
                    }
                    Console.WriteLine("Q. Quit Application");

                    while (selectedGame is null)
                    {
                        Console.WriteLine();
                        switch (Console.ReadKey().KeyChar)
                        {
                            case 'q':
                            case 'Q':
                                return;
                            case char c when char.IsDigit(c):
                                var selection = int.Parse(c.ToString());
                                if (selection > gameList.Count || selection < 1)
                                {
                                    Console.WriteLine("Invalid selection, please try again");
                                    continue;
                                }
                                selectedGame = gameDict[gameList[selection - 1]];
                                break;
                            default:
                                Console.WriteLine("Invalid selection, please try again");
                                continue;
                        }
                    }
                }
                catch
                {

                }
            }
            if (selectedGame is null)
            {
                Console.WriteLine("No game was selected, please try running this application again");
                return;
            }
            PatchAcf(selectedGame);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Console.WriteLine("Finished. Press any key to exit");
        Console.ReadKey();
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
    private static string? GetManifestDirectory(GameInfo game)
    {
        try
        {
            if (game is null)
                return null;
            var fileName =$"appmanifest_{game.AppId}.acf";
            var pathReplaceStr = $"common\\{game.Name}";
            if (File.Exists(game.Binary) && Environment.CurrentDirectory.Contains(pathReplaceStr))
            {
                Console.WriteLine($"File was ran from the game directory, using {Environment.CurrentDirectory}");
                var fullPath =  Path.Combine(Environment.CurrentDirectory.Replace(pathReplaceStr, ""), fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            

            string? installDir = null;
            foreach (var registryKeyPath in game.RegistryLocations)
            {
                installDir = GetInstallDirFromRegistry(registryKeyPath);
                if (!string.IsNullOrEmpty(installDir))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(installDir))
            {
                Console.WriteLine($"{game.Name} installation directory not found in registry.");
                return null;
            }

            // Construct the path to the Steam manifest file
            string steamManifestPath = Path.Combine(installDir.Replace(pathReplaceStr, ""), fileName);

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

    private static void PatchAcf(GameInfo? game)
    {
        var steamManifestPath = GetManifestDirectory(game) ?? throw new Exception("Could not locate the manifest file on your machine. Place this exe in your fallout 4 directory for detection");
        Console.Clear();
        Console.WriteLine("\n");
        Console.WriteLine($"Manifest Location: {steamManifestPath}");
        var backupFile = $"{steamManifestPath}.backup_{DateTime.Now:ddmmyyyyhhMMss}";
        Console.WriteLine($"Backup Location: {backupFile}");
        Console.WriteLine("\nPress Y to start or any other key to abort.");
        var key = Console.ReadKey();
        Console.WriteLine();
        if (key.Key != ConsoleKey.Y)
        {
            Console.WriteLine($"Execution aborted, press any key to exit");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Shutting down steam");
        var steamFilePath = KillProcess("Steam");

        File.Copy(steamManifestPath, backupFile, true);
        Console.WriteLine($"Manifest file backed up to {backupFile}");

        var acf = AppState.Deserialize(steamManifestPath);
        bool hasChanges = false;
        foreach (var item in game.Main)
        {
            if (acf.Main.TryGetValue(item.Key, out var val))
            {
                if (val != item.Value)
                {
                    acf.Main[item.Key] = item.Value;
                    Console.WriteLine($"Changed value for {item.Key} from {val} to {item.Value}");
                    hasChanges = true;
                }
                continue;
            }
            else
            {
                acf.Main[item.Key] = item.Value;
                Console.WriteLine($"Setting was missing. Added key {item.Key} with value {item.Value}");
                hasChanges = true;
            }
        }
        foreach (var item in game.DepotManifests)
        {
            if (acf.InstalledDepots.TryGetValue(item.Key, out var val))
            {
                if (val.manifest != item.Value)
                {
                    Console.WriteLine($"Changed Depo Manifest value for {item.Key} from {val.manifest} to {item.Value}");
                    acf.InstalledDepots[item.Key].manifest = item.Value;
                    hasChanges = true;
                }
                continue;
            }
            else
            {
                acf.Main[item.Key] = item.Value;
                Console.WriteLine($"Setting was missing. Added key {item.Key} with value {item.Value}");
                hasChanges = true;
            }
        }
        if (hasChanges)
        {
            RemoveReadOnly(steamManifestPath);
            File.Delete(steamManifestPath);
            File.WriteAllText(steamManifestPath, AppState.Serialize(acf));
            SetReadOnly(steamManifestPath);
        }
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

        Console.WriteLine($"Manifest file {(hasChanges ? "updated successfully": "Required no changes (file is not modified)")}.\n{(started ? "Steam restarted" : "Could not start steam, please launch manually")}");
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
