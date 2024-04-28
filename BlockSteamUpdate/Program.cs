using Microsoft.Win32;
using SteamSkipNextGenUpdate.Models;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

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
                }
                catch { }
            }
            try
            {
                WriteConsole("\n\n");

                WriteConsole("=======================================");
                WriteConsole($"ACF File Patcher - By Bilago v{Assembly.GetEntryAssembly()?.GetName().Version}");
                WriteConsole("=======================================");
                WriteConsole();
                if (gameList.Count == 0) 
                    throw new Exception("No Game .Json found, please download this tool again and include all the files in the archive.");

                WriteConsole("Choose a game to prevent steam updates:");
                WriteConsole();
                var place = 1;
                foreach (var g in gameList)
                {
                    WriteConsole($"{place++}. {g}");
                }
                WriteConsole("Q. Quit Application");

                while (selectedGame is null)
                {
                    Console.Write(InputHeader);
                    switch (Console.ReadKey().KeyChar)
                    {
                        case 'q':
                        case 'Q':
                            return;
                        case char c when char.IsDigit(c):
                            var selection = int.Parse(c.ToString());
                            if (selection > gameList.Count || selection < 1)
                            {
                                WriteConsole("\nInvalid selection, please try again");
                                continue;
                            }
                            selectedGame = gameDict[gameList[selection - 1]];
                            break;
                        default:
                            WriteConsole("\nInvalid selection, please try again");
                            continue;
                    }
                    WriteConsole();
                }
            }
            catch
            {

            }
            if (selectedGame is null)
            {
                WriteConsole("No game was selected, please try running this application again");
                return;
            }
            PatchAcf(selectedGame);
        }
        catch (Exception ex)
        {
            WriteConsole(ex.Message, ConsoleColor.Red, false);
        }
        finally
        {
            WriteConsole();
            WriteConsole("Finished");
            WriteConsole($"Press L to save the output of this tool to {LogFile}");
            WriteConsole($"Press any other key to exit");
            WriteConsole();
            Console.Write(InputHeader);
            switch(Console.ReadKey().Key)
            {
                case ConsoleKey.Q:
                    break;
                case ConsoleKey.L:
                    File.WriteAllLines(LogFile, Output);
                    break;
            }
        }
    }
    private static string LogFile = $"AcfPatchTool_{DateTime.Now:MM-dd-yyyy hhmmss}.log";

    private static void RestoreBacup(List<FileInfo> backFiles, string manifestFile)
    {
        try
        {
            Console.Clear();
            if (backFiles.Count == 0)
            {
                WriteConsole("No backups were found");
                return;
            }
            var selectedBackup = backFiles.First();
            if (backFiles.Count > 1)
            {
                selectedBackup = null;
                WriteConsole();
                WriteConsole("Pick which backup you would like to use");

                foreach (var backup in backFiles)
                {
                    WriteConsole($"{backFiles.IndexOf(backup) + 1}. {backup.Name} - Modified on {backup.LastWriteTime}");
                }
                WriteConsole("Q. Quit application");
                WriteConsole();
                
                while (selectedBackup is null)
                {
                    Console.Write(InputHeader);
                    var input = Console.ReadLine();
                    if (input?.ToLower() == "q")
                        return;
                    if (!int.TryParse(input, out var num) || num < 1 || num > backFiles.Count)
                    {
                        WriteConsole("Invalid selection, try again");
                        continue;
                    }
                    selectedBackup = backFiles[num-1];
                }

                WriteConsole($"Manifest file: {manifestFile}");
                WriteConsole($"Manifest to restore: {selectedBackup}");

                if (!AreYouSure())
                    return;
                WriteConsole();
                ReplaceFile(selectedBackup.FullName, manifestFile);
                WriteConsole("File has been restored! Restarting steam");
                if (!StartProcess(KillProcess("Steam")))
                    WriteConsole("Could not start steam", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            WriteConsole(ex.Message, ConsoleColor.Red, false);
        }
    }

    private static void ReplaceFile(string file, string destination)
    {
        RemoveReadOnly(destination);
        File.Copy(file, destination, true);
        SetReadOnly(destination);
    }

    private static bool StartProcess(string? filePath)
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
    private static bool AreYouSure()
    {
        WriteConsole();
        WriteConsole("Press Y to continue, any other key to abort");
        Console.Write(InputHeader);
        return Console.ReadKey().Key == ConsoleKey.Y;
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
            WriteConsole($"Set {file} to read only");
            
        }
        catch (Exception ex)
        {
            WriteConsole(ex.Message, ConsoleColor.Red, false);
        }
    }
    private static string TraverseDirectory(string path, int count)
    {
        var directoryInfo = new DirectoryInfo(path);
        for (int i = 0; i < count && directoryInfo.Parent != null; i++)
        {
            directoryInfo = directoryInfo.Parent;
        }
        return directoryInfo.FullName;
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
            WriteConsole($"Removed Read-Only attribute from {file}");
        }
        catch (Exception ex)
        {
            WriteConsole(ex.Message);
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
            if (File.Exists(game.Binary) && Environment.CurrentDirectory.Contains("steamapps"))
            {
                WriteConsole($"File was ran from the game directory, using {Environment.CurrentDirectory}");

                var fullPath = Path.Combine(TraverseDirectory(Environment.CurrentDirectory, 2), fileName);
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
                WriteConsole($"{game.Name} installation directory not found in registry.", ConsoleColor.Red);
                return null;
            }
            
            // Construct the path to the Steam manifest file
            string steamManifestPath = Path.Combine(TraverseDirectory(installDir, 2), fileName);

            if (!File.Exists(steamManifestPath))
            {
                WriteConsole("Steam manifest file not found.", ConsoleColor.Red);
                return null;
            }
            return steamManifestPath;
        }
        catch(Exception ex)
        {
            WriteConsole(ex.Message, ConsoleColor.Red, false);
            return null;
        }
    }
    private const string InputHeader = "\t\t\t\t> ";
    private static void PatchAcf(GameInfo? game)
    {
        var steamManifestPath = 
            GetManifestDirectory(game) ?? 
            throw new Exception($"Could not locate the manifest file on your machine. Place this exe in your {game.Name} directory containing {game.Binary} for detection");
        Console.Clear();
        WriteConsole("\n");
        WriteConsole($"Manifest Location: {steamManifestPath}", center: false);


        var backFiles = Directory.GetFiles(Path.GetDirectoryName(steamManifestPath)??Environment.CurrentDirectory, "*.acf.backup*").Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
        WriteConsole();
        WriteConsole($"Game: {game.Name}");
        WriteConsole("Make a selection");
        WriteConsole();
        if (backFiles.Count() > 0)
            WriteConsole("R. Restore Backup ACF file");
        WriteConsole($"Y. Patch {game.Name} acf");
        WriteConsole();
        WriteConsole($"Press any other key to abort");
        Console.Write(InputHeader);
        var key = Console.ReadKey().Key;
        WriteConsole();
        switch (key)
        {
            case ConsoleKey.R:
                RestoreBacup(backFiles, steamManifestPath);
                return;
            case ConsoleKey.Y:
                break;
            default:
                WriteConsole($"Execution aborted, press any key to exit");
                return;
        }

        WriteConsole($"Reading {steamManifestPath}");
        var acf = AppState.Deserialize(steamManifestPath);
        if (acf is null)
        {
            WriteConsole("Error Deserializing File, cannot be patched. \nAborting progress, no changes have been made. Try using the manual method instead.", ConsoleColor.Red, false);
            WriteConsole("Press Any key to continue");
            Console.ReadKey();
            return;
        }


        bool hasChanges = false;
        foreach (var item in game.Main)
        {
            if (acf.Main.TryGetValue(item.Key, out var val))
            {
                if (val != item.Value)
                {
                    acf.Main[item.Key] = item.Value;
                    WriteConsole($"Changed value for {item.Key} from {val} to {item.Value}", ConsoleColor.Green);
                    hasChanges = true;
                }
                continue;
            }
            else
            {
                acf.Main[item.Key] = item.Value;
                WriteConsole($"Setting was missing. Added key {item.Key} with value {item.Value}", ConsoleColor.Yellow);
                Console.ResetColor();
                hasChanges = true;
            }
        }
        var depoCount = 0;
        var languageDepotFound = game.LanguageSpecificDepots.Count == 0;
        foreach (var item in game.DepotManifests)
        {
            var isMandatory = game.MandatoryDepots.Contains(item.Key);
            var isLanguageDepot = game.LanguageSpecificDepots.ContainsKey(item.Key);
            var isDlcDepot = game.DLCDepots.ContainsKey(item.Key);
            if (acf.InstalledDepots.TryGetValue(item.Key, out var val))
            {
                //at least one language depot found
                if (isLanguageDepot)
                {
                    WriteConsole($"Language Depot found in acf: {game.LanguageSpecificDepots[item.Key]}: {item.Key}", ConsoleColor.Green);
                    languageDepotFound = true;
                }

                if (val.manifest != item.Value)
                {
                    WriteConsole($"Changed DepotManifest value for {item.Key} from {val.manifest} to {item.Value}", ConsoleColor.Green);
                    acf.InstalledDepots[item.Key].manifest = item.Value;
                    depoCount++;
                    hasChanges = true;
                }
                continue;
            }
            else
            {
                if (isLanguageDepot)
                    continue;
                else if (isMandatory)
                    WriteConsole($"Depot {item.Key} was missing from your acf file. This patch might not be compatible with your version", ConsoleColor.Red, false);
                else if (isDlcDepot)
                {
                    WriteConsole($"Depot {item.Key} is missing from your acf file, this is for the DLC: {game.DLCDepots[item.Key]}", ConsoleColor.Yellow, false);
                    WriteConsole($"If you do not own this DLC, this is not an error", ConsoleColor.Yellow, false);
                }
                else
                    WriteConsole($"Skipping Depot {item.Key}. Missing from your ACF but not required", ConsoleColor.Gray);
            }
        }
        if (!languageDepotFound)
            WriteConsole($"No language depot for {game.Name} in your acf file. This patch might not be compatible with your version", ConsoleColor.Red, false);
        var missingDepots = acf.InstalledDepots.Where(x => !game.DepotManifests.ContainsKey(x.Key));
        if (missingDepots.Any())
        {
            WriteConsole();
            WriteConsole("===============================================================");
            WriteConsole($"The following Depots were found in your ACF but not defined in the patch.");
            WriteConsole($"This is not an error but information in case the patch doesn't work");
            WriteConsole("===============================================================");
            WriteConsole();
            foreach (var depot in missingDepots)
            {
                var sb = new StringBuilder();
                sb.Append($"DepotId: {depot.Key} Manifest: {depot.Value.manifest}");
                if (game.DLCDepots.TryGetValue(depot.Key, out var dlcName))
                    sb.Append($" DLC: {dlcName}");
                if (game.LanguageSpecificDepots.TryGetValue(depot.Key, out var language))
                    sb.Append($" Language: {language}");

                WriteConsole(sb.ToString(), ConsoleColor.Gray);
            }
            WriteConsole("===============================================================");
            WriteConsole();
        }
        string? steamFilePath = null;
        var steamRestarted = false;
        var steamKilled = false;
        if (hasChanges)
        {
            var text = AppState.Serialize(acf);
            if (string.IsNullOrEmpty(text))
            {
                WriteConsole($"Error patching file. Aborting progress, no changes have been made. Try using the manual method instead.", ConsoleColor.Red, false);
                WriteConsole("Press Any key to continue");
                Console.ReadKey();
                return;
            }

            var backupFile = $"{steamManifestPath}.backup_{DateTime.Now:ddMMyyyyhhmmss}";
            WriteConsole($"Created Backup File: {backupFile}", center: false);

            WriteConsole("Shutting down steam");
            steamFilePath = KillProcess("Steam");
            steamKilled = !string.IsNullOrEmpty(steamFilePath);
            if (!steamKilled && File.Exists(acf.LauncherPath))
                steamFilePath = acf.LauncherPath;

            File.Copy(steamManifestPath, backupFile, true);
            WriteConsole($"Manifest file backed up to {backupFile}");

            if (File.Exists(steamManifestPath))
            {
                RemoveReadOnly(steamManifestPath);
                File.Delete(steamManifestPath);
            }
            File.WriteAllText(steamManifestPath, text);
            if(!File.Exists(steamManifestPath))
            {
                WriteConsole($"File failed to write, restoring backup", ConsoleColor.Red);
                File.Copy(steamManifestPath, backupFile, true);
            }
            SetReadOnly(steamManifestPath);
            steamRestarted = RestartSteam(steamFilePath);
        }

        WriteConsole($"Manifest file {(hasChanges ? "updated successfully" : "required no changes (file was not patched)")}", ConsoleColor.Green);
        if (steamKilled)
            WriteConsole(steamRestarted ? "Steam has been restarted" : "Could not start steam, please launch manually");
    }
    private static List<string> Output = new List<string>();
    private static void WriteConsole(string? message = null, ConsoleColor? c = null, bool center = true)
    {
        try
        {
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine();
                return;
            }
            if (c.HasValue)
                Console.ForegroundColor = c.Value;
            if (center) message = "\t\t\t\t" + message;
            Console.WriteLine(message);
            if (c.HasValue)
                Console.ResetColor();
            Output.Add(message.Trim('\t'));
        }
        catch(Exception ex)
        {
            Console.WriteLine(message);
        }
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
                WriteConsole(ex.Message);
            }
        }
        return null;
    }
    private static bool RestartSteam(string filePath)
    {
        if (filePath != null && File.Exists(filePath))
        {
            WriteConsole("Starting Steam");
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
                    WriteConsole("Starting Steam");
                    Process.Start("C:\\Program Files\\Steam\\Steam.exe");
                    return true;
                }
            }
        }
        return false;
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
            WriteConsole($"Error reading registry: {ex.Message}");
        }
        return null;
    }
}
