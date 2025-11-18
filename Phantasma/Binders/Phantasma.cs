using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using IronScheme;
using IronScheme.Runtime;

using Phantasma.Models;
using Phantasma.Views;

namespace Phantasma.Binders;

public class Phantasma
{
    private Dictionary<string, string> Configuration;
    private Common Common;
    private Dimensions Dimensions;

    private SplashWindow splashWindow;
    
    private static string LoadFileName = "";

    private int FullScreenMode = 0;
    private int DeveloperMode = 0;
    private int ExitProgram = 0;
    private int PrintOptions = 0;

    private static string ProgramName = "Phantasma";
    private static string PackageVersion = "1.0";

    public Phantasma(string[] args)
    {
        Common = new Common();
        Configuration = new Dictionary<string, string>();
        
        ParseArgs(args);
    }

    /// <summary>
    /// Initialize with Avalonia application.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Show splash screen while loading.
        await ShowSplashScreen();
    }

    /// <summary>
    /// Show splash screen with loading progress.
    /// </summary>
    private async Task ShowSplashScreen()
    {
        splashWindow = new SplashWindow();
        
        // Load splash image based on screen dimensions.
        string splashImagePath = GetSplashImagePath();
        await splashWindow.LoadSplashImage(splashImagePath);
        
        // Show splash and perform loading.
        await splashWindow.ShowSplashAsync(async (progress) =>
        {
            await LoadGameResources(progress);
        });
    }

    /// <summary>
    /// Get the appropriate splash image path based on screen dimensions.
    /// </summary>
    private string GetSplashImagePath()
    {
        string dims = Configuration.GetValueOrDefault("screen-dims", "640x480");
        const string suffix = "-splash-image-filename";
        string key = string.Concat(dims, suffix);
        
        // Default splash image names for different resolutions.
        var splashImages = new Dictionary<string, string>
        {
            { "640x480-splash-image-filename", "splash_640x480.png" },
            { "800x600-splash-image-filename", "splash_800x600.png" },
            { "1024x768-splash-image-filename", "splash_1024x768.png" },
            { "1280x720-splash-image-filename", "splash_1280x720.png" },
            { "1920x1080-splash-image-filename", "splash_1920x1080.png" }
        };
        
        string baseName = splashImages.GetValueOrDefault(key, "splash_default.png");
        
        // Look for the splash image in various locations.
        string[] searchPaths = 
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", baseName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseName),
            Path.Combine(Configuration.GetValueOrDefault("include-dirname", ""), baseName),
            baseName
        };
        
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"Found splash image: {path}");
                return path;
            }
        }
        
        Console.WriteLine($"No splash image found for {dims}");
        return null;
    }

    /// <summary>
    /// Load all game resources with progress reporting.
    /// </summary>
    private async Task LoadGameResources(IProgress<(double, string)> progress)
    {
        try
        {
            // Initialize IronScheme.
            progress.Report((10, "Initializing scripting engine..."));
            await Task.Run(() => InitializeScheme());
            
            // Load game data.
            progress.Report((20, "Loading game data..."));
            await Task.Run(() => LoadGameData());
            
            // Load sprites and graphics.
            progress.Report((40, "Loading graphics..."));
            await Task.Run(() => LoadGraphics());
            
            // Load sounds.
            progress.Report((50, "Loading sounds..."));
            await Task.Run(() => LoadSounds());
            
            // Load maps.
            progress.Report((60, "Loading maps..."));
            await Task.Run(() => LoadMaps());
            
            // Load scripts.
            progress.Report((70, "Loading scripts..."));
            await Task.Run(() => LoadScripts());
            
            // Initialize game session.
            progress.Report((85, "Initializing game session..."));
            //await Task.Run(() => InitializeSession());
            
            // Final initialization.
            progress.Report((95, "Starting Phantasma..."));
            await Task.Delay(5000); // Small delay for effect
            
            progress.Report((100, "Complete!"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during game loading: {ex.Message}");
            throw;
        }
    }

    private void InitializeConfig()
    {
        // Set default configuration values.
        if (!Configuration.ContainsKey("screen-dims"))
            Configuration["screen-dims"] = "1024x768";
            
        if (!Configuration.ContainsKey("include-dirname"))
            Configuration["include-dirname"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            
        if (!Configuration.ContainsKey("saved-games-dirname"))
            Configuration["saved-games-dirname"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "Phantasma", 
                "Saves");
            
        if (!Configuration.ContainsKey("sound-enabled"))
            Configuration["sound-enabled"] = "yes";
            
        // Create directories if they don't exist.
        Directory.CreateDirectory(Configuration["include-dirname"]);
        Directory.CreateDirectory(Configuration["saved-games-dirname"]);
    }

    private void InitializeScheme()
    {
        // Initialize IronScheme environment.
        Console.WriteLine("Initializing IronScheme...");
        // TODO: Set up Scheme environment
    }

    private void LoadGameData()
    {
        Console.WriteLine("Loading game data...");
        // TODO: Load data files
    }

    private void LoadGraphics()
    {
        Console.WriteLine("Loading graphics...");
        // TODO: Load sprite sheets and tiles
    }

    private void LoadSounds()
    {
        if (Configuration.GetValueOrDefault("sound-enabled", "yes") == "yes")
        {
            Console.WriteLine("Loading sounds...");
            // TODO: Load sound files
        }
    }

    private void LoadMaps()
    {
        Console.WriteLine("Loading maps...");
        // TODO: Load map data
    }

    private void LoadScripts()
    {
        Console.WriteLine("Loading scripts...");
        if (!string.IsNullOrEmpty(LoadFileName))
        {
            Console.WriteLine($"Loading save file: {LoadFileName}");
            // TODO: Load save game
        }
        else
        {
            Console.WriteLine("Loading default game scripts...");
            // TODO: Load default game
        }
    }

    private static void PrintVersion()
    {
        Console.Write("{0} {1}\r\n", ProgramName, PackageVersion);
        Console.Write("Ported from Nazghul, Copyright (C) 2003 Gordon McNutt & Sam Glasby\r\n"+
                      "to .NET in 2025 by Nathan Papke.\r\n"+
                      "{0} comes with NO WARRANTY,\r\n"+
                      "to the extent permitted by law.\r\n"+
                      "You may redistribute copies of {0}\r\n"+
                      "under the terms of the GNU General Public License.\r\n"+
                      "For more information about these matters,\r\n"+
                      "see the file named COPYING.\r\n",
            ProgramName);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  {0} [options] <load-file>", ProgramName);
        Console.WriteLine("Options:");
        Console.WriteLine("    -b: bot"); //added
        Console.WriteLine("    -h: help);");
        Console.WriteLine("    -l: line of sight <floodfill|angband>"); //added
        Console.WriteLine("    -v: version");
        Console.WriteLine("    -d: developer mode");
        Console.WriteLine("    -f: fullscreen mode);");
        Console.WriteLine("    -t: tick <period in msec>");
        Console.WriteLine("    -a: animation <period in ticks>");
        Console.WriteLine("    -s: sound <0 to disable>");
        Console.WriteLine("    -R: recorder <filename>");
        Console.WriteLine("    -P: playback <filename>");
        Console.WriteLine("    -S: speed <playback ms delay>");
        Console.WriteLine("    -I: game data dir");
        Console.WriteLine("    -G: save game dir");
        Console.WriteLine("    -r: screen size <pixels> (eg, 640x480))):" );
        Console.WriteLine("    -T: show all terrain");
        Console.WriteLine("<load-file> is the session to load.");
    }

    private void ParseArgs(string[] args)
    {
        int c = 0;
        
        Common.TickMilliseconds = Common.MS_PER_TICK;
        Common.AnimationTicks = Common.ANIMATION_TICKS;

        while (c < args.Length && args[c].Substring(0, 1) == "-")
        {
            switch (args[c].Substring(1, 1))
            {
                case "t":
                    Common.TickMilliseconds = int.Parse(args[c].Substring(3));
                    break;
                case "a":
                    Common.AnimationTicks = int.Parse(args[c].Substring(3));
                    break;
                case "s":
                    Configuration["sound-enabled"] = int.Parse(args[c].Substring(3)) != 0 ? "yes" : "no";
                    break;
                case "T":
                    Common.ShowAllTerrain = 1;
                    break;
                case "d":
                    DeveloperMode = 1;
                    //DEBUG = 1;
                    //VERBOSE = 1;
                break;
                case "f":
                    FullScreenMode = 1;
                    break;
                case "R":
                    // Set the filename for recording keystrokes.
                    Configuration["record-filename"] = args[c].Substring(3);
                    break;
                case "S":
                    // Set the speed to play back recorded keystrokes.
                    Configuration["playback-speed"] = args[c].Substring(3);
                    break;
                case "P":
                    // Set the file to play back keystrokes from.
                    Configuration["playback-filename"] = args[c].Substring(3);
                    break;
                case "I":
                    // Set the directory for read-only game and cfg files.
                    Configuration["include-dirname"] = args[c].Substring(3);
                    break;
                case "G":
                    // Set the directory for read-write game and cfg files.
                    Configuration["saved-games-dirname"] = args[c].Substring(3);
                    break;
                case "v":
                    PrintVersion();
                    Environment.Exit(0);
                    break;
                case "h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                case "o":
                    PrintOptions = 1;
                    break;
                case "r":
                    // Set the screen dimensions.
                    Configuration.Add("screen-dims", args[c].Substring(3));
                    break;
                default:
                    PrintUsage();
                    Environment.Exit(-1);
                    break;
            }

            c++;
        }
        
        // --------------------------------------------------------------------
        // Any remaining option is assumed to be the save-file to load the game
        // from. If there is none then abort.
        // --------------------------------------------------------------------
        if (c < args.Length)
            LoadFileName = args[c];
    }

    /// <summary>
    /// Check if developer mode is enabled.
    /// </summary>
    public bool IsDeveloperMode()
    {
        return DeveloperMode == 1;
    }

    /// <summary>
    /// Check if fullscreen mode is requested.
    /// </summary>
    public bool IsFullScreenMode()
    {
        return FullScreenMode == 1;
    }
}