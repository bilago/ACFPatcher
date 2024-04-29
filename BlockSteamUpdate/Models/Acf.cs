public class InstalledDepot
{
    public string manifest { get; set; }
    public string size { get; set; }
    public string dlcappid { get; set; }
}
public class AppState
{
    public string appid
    {
        get
        {
            return Main[nameof(appid)];
        }
        set
        {
            Main[nameof(appid)] = value;
        }
    }
    public string universe
    {
        get
        {
            return Main[nameof(universe)];
        }
        set
        {
            Main[nameof(universe)] = value;
        }
    }
    public string LauncherPath
    {
        get
        {
            return Main[nameof(LauncherPath)];
        }
        set
        {
            Main[nameof(LauncherPath)] = value;
        }
    }
    public string name
    {
        get
        {
            return Main[nameof(name)];
        }
        set
        {
            Main[nameof(name)] = value;
        }
    }
    public string StateFlags
    {
        get
        {
            return Main[nameof(StateFlags)];
        }
        set
        {
            Main[nameof(StateFlags)] = value;
        }
    }
    public string installdir
    {
        get
        {
            return Main[nameof(installdir)];
        }
        set
        {
            Main[nameof(installdir)] = value;
        }
    }
    public string LastUpdated
    {
        get
        {
            return Main[nameof(LastUpdated)];
        }
        set
        {
            Main[nameof(LastUpdated)] = value;
        }
    }
    public string LastPlayed
    {
        get
        {
            return Main[nameof(LastPlayed)];
        }
        set
        {
            Main[nameof(LastPlayed)] = value;
        }
    }
    public string SizeOnDisk
    {
        get
        {
            return Main[nameof(SizeOnDisk)];
        }
        set
        {
            Main[nameof(SizeOnDisk)] = value;
        }
    }
    public string StagingSize
    {
        get
        {
            return Main[nameof(StagingSize)];
        }
        set
        {
            Main[nameof(StagingSize)] = value;
        }
    }
    public string buildid
    {
        get
        {
            return Main[nameof(buildid)];
        }
        set
        {
            Main[nameof(buildid)] = value;
        }
    }
    public string LastOwner
    {
        get
        {
            return Main[nameof(LastOwner)];
        }
        set
        {
            Main[nameof(LastOwner)] = value;
        }
    }
    public string UpdateResult
    {
        get
        {
            return Main[nameof(UpdateResult)];
        }
        set
        {
            Main[nameof(UpdateResult)] = value;
        }
    }
    public string BytesToDownload
    {
        get
        {
            return Main[nameof(BytesToDownload)];
        }
        set
        {
            Main[nameof(BytesToDownload)] = value;
        }
    }
    public string BytesDownloaded
    {
        get
        {
            return Main[nameof(BytesDownloaded)];
        }
        set
        {
            Main[nameof(BytesDownloaded)] = value;
        }
    }
    public string BytesToStage
    {
        get
        {
            return Main[nameof(BytesToStage)];
        }
        set
        {
            Main[nameof(BytesToStage)] = value;
        }
    }
    public string BytesStaged
    {
        get
        {
            return Main[nameof(BytesStaged)];
        }
        set
        {
            Main[nameof(BytesStaged)] = value;
        }
    }
    public string TargetBuildID
    {
        get
        {
            return Main[nameof(TargetBuildID)];
        }
        set
        {
            Main[nameof(TargetBuildID)] = value;
        }
    }
    public string AutoUpdateBehavior
    {
        get
        {
            return Main[nameof(AutoUpdateBehavior)];
        }
        set
        {
            Main[nameof(AutoUpdateBehavior)] = value;
        }
    }
    public string AllowOtherDownloadsWhileRunning
    {
        get
        {
            return Main[nameof(AllowOtherDownloadsWhileRunning)];
        }
        set
        {
            Main[nameof(AllowOtherDownloadsWhileRunning)] = value;
        }
    }
    public string ScheduledAutoUpdate
    {
        get
        {
            return Main[nameof(ScheduledAutoUpdate)];
        }
        set
        {
            Main[nameof(ScheduledAutoUpdate)] = value;
        }
    }
    public Dictionary<string, InstalledDepot> InstalledDepots { get; set; } = new();
    public Dictionary<string, string> InstallScripts { get; set; } = new();
    public Dictionary<string, string> SharedDepots { get; set; } = new();
    public Dictionary<string, string> UserConfig { get; set; } = new();
    public Dictionary<string, string> MountedConfig { get; set; } = new();
    public Dictionary<string, string> Main { get; set; } = new();


    public static AppState? Deserialize(string filePath)
    {
        try
        {
            var appState = new AppState();
            var installedDepots = new Dictionary<string, InstalledDepot>();
            var installScripts = new Dictionary<string, string>();
            var sharedDepots = new Dictionary<string, string>();
            var userConfig = new Dictionary<string, string>();
            var mountedConfig = new Dictionary<string, string>();
            var main = new Dictionary<string, string>();

            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = reader?.ReadLine()) != null)
            {
                if (line.Contains('{') || line.Contains('}'))
                    continue;

                var keyValue = ToKVP(line);
                var key = keyValue.Key;
                if (keyValue.Value is not null)
                {
                    main[key] = keyValue.Value;
                    continue;
                }
                var bracketCount = 0;
                switch (key)
                {
                    case "InstalledDepots":
                        string? currentDepoId = null;
                        while ((line = reader?.ReadLine()) != null)
                        {
                            if (line.Contains('{'))
                            {
                                bracketCount++;
                                continue;
                            }
                            else if (line.Contains('}'))
                            {
                                bracketCount--;
                                if (bracketCount == 0)
                                    break;
                                continue;
                            }

                            var kv = ToKVP(line);
                            var k = kv.Key;
                            if (kv.Value is null)
                            {
                                currentDepoId = k;
                                continue;
                            }
                            if (currentDepoId is null)
                                continue;
                            var v = kv.Value;
                            if (!installedDepots.ContainsKey(currentDepoId))
                                installedDepots[currentDepoId] = new();
                            switch (k)
                            {
                                case "manifest":
                                    installedDepots[currentDepoId].manifest = v;
                                    break;
                                case "size":
                                    installedDepots[currentDepoId].size = v;
                                    break;
                                case "dlcappid":
                                    installedDepots[currentDepoId].dlcappid = v;
                                    break;
                            }
                        }
                        break;
                    case "UserConfig":
                        ParseDictionary(reader, userConfig);
                        break;
                    case "SharedDepots":
                        ParseDictionary(reader, sharedDepots);
                        break;
                    case "MountedConfig":
                        ParseDictionary(reader, mountedConfig);
                        break;
                    case "InstallScripts":
                        ParseDictionary(reader, installScripts);
                        break;
                }
            }

            appState.InstalledDepots = installedDepots;
            appState.InstallScripts = installScripts;
            appState.SharedDepots = sharedDepots;
            appState.UserConfig = userConfig;
            appState.MountedConfig = mountedConfig;
            appState.Main = main;
            return appState;
        }
        catch(Exception ex)
        {
            Program.WriteConsole(ex.Message, ConsoleColor.Red);
            return null;
        }
    }

    private static void ParseDictionary(StreamReader? reader, Dictionary<string, string> dict)
    {
        if (reader is null)
            return;
        dict ??= new();
        var bracketCount = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Contains('{'))
            {
                bracketCount++;
                continue;
            }
            else if (line.Contains('}'))
            {
                bracketCount--;
                if (bracketCount == 0)
                    break;
                continue;
            }
            var kv = ToKVP(line);
            dict[kv.Key] = kv.Value;
        }
    }

    private static KeyValuePair<string, string?> ToKVP(string line)
    {
        var res = line?.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)?.Select(x => x.Trim('\"'));
        if (res is null)
            return new KeyValuePair<string, string?>("", null);
        if (res?.Count() == 1)
            return new KeyValuePair<string, string?>(res.First(), null);
        return new KeyValuePair<string, string?>(res.First(), res.Last());
    }

    public static string? Serialize(AppState appState)
    {
        try
        {
            StringWriter sw = new();

            // Start the AppState section
            sw.WriteLine("\"AppState\"");
            sw.WriteLine("{");
            foreach (var item in appState.Main)
            {
                SerializeProperty(sw, item.Key, item.Value);
            }

            // Serialize InstalledDepots
            sw.WriteLine("\t\"InstalledDepots\"");
            sw.WriteLine("\t{");
            foreach (var depot in appState.InstalledDepots)
            {
                sw.WriteLine($"\t\t\"{depot.Key}\"");
                sw.WriteLine("\t\t{");
                SerializeProperty(sw, "manifest", depot.Value.manifest, 3);
                SerializeProperty(sw, "size", depot.Value.size, 3);
                SerializeProperty(sw, "dlcappid", depot.Value.dlcappid, 3);
                sw.WriteLine("\t\t}");
            }
            sw.WriteLine("\t}");

            // Serialize InstallScripts
            sw.WriteLine("\t\"InstallScripts\"");
            sw.WriteLine("\t{");
            foreach (var script in appState.InstallScripts)
            {
                SerializeProperty(sw, script.Key, script.Value, 2);
            }
            sw.WriteLine("\t}");

            // Serialize SharedDepots
            sw.WriteLine("\t\"SharedDepots\"");
            sw.WriteLine("\t{");
            foreach (var sharedDepot in appState.SharedDepots)
            {
                SerializeProperty(sw, sharedDepot.Key, sharedDepot.Value, 2);
            }
            sw.WriteLine("\t}");

            // Serialize UserConfig
            sw.WriteLine("\t\"UserConfig\"");
            sw.WriteLine("\t{");
            foreach(var cfg in appState.UserConfig)
            {
                SerializeProperty(sw, cfg.Key, cfg.Value, 2);
            }          
            sw.WriteLine("\t}");

            // Serialize MountedConfig
            sw.WriteLine("\t\"MountedConfig\"");
            sw.WriteLine("\t{");
            foreach (var mounted in appState.MountedConfig)
            {
                SerializeProperty(sw, mounted.Key, mounted.Value, 2);
            }
            sw.WriteLine("\t}");

            // End the AppState section
            sw.WriteLine("}");

            return sw.ToString();
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static void SerializeProperty(StringWriter sw, string key, string value, int indentationLevel = 1)
    {
        if (value is null)
            return;
        sw.Write(new string('\t', indentationLevel));
        sw.Write($"\"{key}\"");
        sw.Write("\t\t");
        sw.Write($"\"{value}\"");
        sw.WriteLine();
    }

}
