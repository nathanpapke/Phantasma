using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using IronScheme;
using IronScheme.Runtime;

using Phantasma.Models;
using Phantasma.Views;
using Phantasma.Binders;

namespace Phantasma;

public class Phantasma
{
    // ===================================================================
    // SINGLETON PATTERN
    // ===================================================================
    
    private static Phantasma instance;
    
    public static Phantasma Instance 
    { 
        get 
        {
            if (instance == null)
            {
                throw new InvalidOperationException("Phantasma not initialized. Call Initialize() first.");
            }
            return instance;
        }
    }
    
    // ===================================================================
    // SHARED RESOURCES (Singleton)
    // ===================================================================

    private Dictionary<string, string> configuration;
    private Common common;
    private Dimensions dimensions;
    private Kernel kernel;
    private static readonly CombatSounds combatSounds = new();
    
    // Object Registry - maps Scheme tags to C# objects.
    // This is global so all sessions can access defined types.
    private Dictionary<string, object> registeredObjects;

    public static Dictionary<string, string> Configuration => instance.configuration;
    public static Common Common => instance.common;
    public static Dimensions Dimensions => instance.dimensions;
    public static Kernel Kernel => instance.kernel;
    public static CombatSounds CombatSounds => combatSounds;

    /// <summary>
    /// Root directory for all game modules.
    /// </summary>
    public static string GamesDirectory => 
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Games");

    /// <summary>
    /// Full path to the current game's data directory.
    /// All script/asset paths are resolved relative to this.
    /// </summary>
    public static string GameDataDirectory { get; private set; } = "";

    /// <summary>
    /// Name of the currently loaded game.
    /// </summary>
    public static string CurrentGame { get; private set; } = "";
    
    // ===================================================================
    // STATIC CONVENIENCE METHODS (Avoid typing .Instance everywhere)
    // ===================================================================
    
    public static void RegisterObject(string tag, object obj) 
        => instance.registerObject(tag, obj);

    public static object GetRegisteredObject(string tag) 
        => instance.getRegisteredObject(tag);

    public static void LoadSchemeFile(string filename) 
        => instance.loadSchemeFile(filename);

    public static Session CreateAgentSession() 
        => instance.createAgentSession();

    public static void DestroyAgentSession(Session session) 
        => instance.destroyAgentSession(session);
    
    // ===================================================================
    // SESSION MANAGEMENT (Multiple Sessions Allowed)
    // ===================================================================
    
    private Session mainSession;          // The real game
    private List<Session> agentSessions;  // Temporary simulations
    
    public static Session MainSession => instance.mainSession;
    
    // ===================================================================
    // UI & STARTUP
    // ===================================================================
    
    private SplashWindow splashWindow;
    
    private static string loadFileName = "";

    private int fullScreenMode = 0;
    private int developerMode = 0;
    private int exitProgram = 0;
    private int printOptions = 0;

    private static string _programName = "Phantasma";
    private static string _packageVersion = "1.0";

    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    /// <summary>
    /// Initialize Phantasma singleton with command-line arguments.
    /// This should be called once at program startup.
    /// </summary>
    public static void Initialize(string[] args)
    {
        if (instance != null)
        {
            throw new InvalidOperationException("Phantasma already initialized.");
        }
        
        instance = new Phantasma(args);
    }

    public Phantasma(string[] args)
    {
        // Initialize shared resources.
        configuration = new Dictionary<string, string>();
        common = new Common();
        dimensions = new Dimensions();
        registeredObjects = new Dictionary<string, object>();
        agentSessions = new List<Session>();
        
        ParseArgs(args);
        InitializeConfig();
    }

    /// <summary>
    /// Initialize with Avalonia application.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Show splash screen while loading.
        await ShowSplashScreen();
    }

    // ===================================================================
    // OBJECT REGISTRY (For Scheme Interop)
    // ===================================================================
    
    /// <summary>
    /// Register a game object with a Scheme tag.
    /// This makes the object accessible from Scheme code.
    /// Mirrors Nazghul's scm_define() calls in kern-* functions.
    /// 
    /// Objects registered here are available to ALL sessions.
    /// This is for types/templates (terrain types, object types, etc.)
    /// not for instance objects like specific characters.
    /// </summary>
    private void registerObject(string tag, object obj)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Console.WriteLine("Warning: Attempted to register object with null/empty tag");
            return;
        }
        
        if (registeredObjects.ContainsKey(tag))
        {
            Console.WriteLine($"Warning: Overwriting existing object with tag '{tag}'");
        }
        
        registeredObjects[tag] = obj;
    }
    
    /// <summary>
    /// Look up a registered object by its Scheme tag.
    /// </summary>
    private object getRegisteredObject(string tag)
    {
        if (registeredObjects.TryGetValue(tag, out object obj))
        {
            return obj;
        }
    
        // Try case-insensitive match as fallback.
        var key = registeredObjects.Keys.FirstOrDefault(k => 
            string.Equals(k, tag, StringComparison.OrdinalIgnoreCase));
        if (key != null)
        {
            return registeredObjects[key];
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all registered objects (useful for debugging/inspection).
    /// </summary>
    public IReadOnlyDictionary<string, object> GetAllRegisteredObjects()
    {
        return registeredObjects;
    }
    
    // ===================================================================
    // SESSION MANAGEMENT
    // ===================================================================
    
    /// <summary>
    /// Create the main game session.
    /// This should be called after loading game data.
    /// </summary>
    public void CreateMainSession()
    {
        if (mainSession != null)
        {
            return;
        }
        
        // Try to get the world objects from Scheme.
        var place = GetRegisteredObject(Kernel.KEY_CURRENT_PLACE) as Place;
        var player = GetRegisteredObject(Kernel.KEY_PLAYER_CHARACTER) as Character;
        
        // Create session with Scheme objects (or nulls, which will use fallback).
        mainSession = new Session(place, player);
    }
    
    /// <summary>
    /// Get an object from the Scheme environment by name.
    /// Returns null if object doesn't exist or is wrong type.
    /// </summary>
    /// <typeparam name="T">Expected type of the object</typeparam>
    /// <param name="schemeName">Name of the Scheme symbol</param>
    /// <returns>The object, or null if not found/wrong type</returns>
    public T GetSchemeObject<T>(string schemeName) where T : class
    {
        try
        {
            var obj = schemeName.Eval();
            if (obj is T typedObj)
            {
                Console.WriteLine($"[Phantasma] Retrieved {typeof(T).Name} '{schemeName}' from Scheme");
                return typedObj;
            }
            else if (obj != null)
            {
                Console.WriteLine($"[Phantasma] Scheme object '{schemeName}' exists but is type {obj.GetType().Name}, not {typeof(T).Name}");
            }
            else
            {
                Console.WriteLine($"[Phantasma] Scheme object '{schemeName}' not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Phantasma] Error retrieving '{schemeName}' from Scheme: {ex.Message}");
        }
        return null;
    }
    
    /// <summary>
    /// Create a temporary session for agent simulation.
    /// Agents can use this to explore "what-if" scenarios.
    /// </summary>
    private Session createAgentSession() 
    {
        var session = new Session();
        agentSessions.Add(session);
        return session;
    }
    
    /// <summary>
    /// Destroy an agent session when simulation is complete.
    /// </summary>
    private void destroyAgentSession(Session session)
    {
        if (agentSessions.Remove(session))
        {
            session.Stop();
        }
    }
    
    /// <summary>
    /// Destroy all agent sessions.
    /// Useful when starting a new main game.
    /// </summary>
    public void DestroyAllAgentSessions()
    {
        foreach (var session in agentSessions)
        {
            session.Stop();
        }
        agentSessions.Clear();
    }

    // ===================================================================
    // SCHEME INTEGRATION
    // ===================================================================
    
    /// <summary>
    /// Load a Scheme file to configure the game world.
    /// This creates game objects (terrains, places, etc.) via kern-* functions.
    /// </summary>
    private void loadSchemeFile(string filename)
    {
        if (kernel == null)
        {
            Console.WriteLine($"Error: Cannot load Scheme file '{filename}' - kernel not initialized");
            return;
        }
        
        try
        {
            kernel.LoadSchemeFile(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Scheme file '{filename}': {ex.Message}");
        }
    }

    // ===================================================================
    // SPLASH SCREEN & LOADING
    // ===================================================================

    /// <summary>
    /// Show splash screen with loading progress.
    /// </summary>
    private async Task ShowSplashScreen()
    {
        splashWindow = new SplashWindow();
        
        // Load splash image based on screen dimensions.
        string splashImagePath = GetSplashImagePath();
        await splashWindow.LoadSplashImage(splashImagePath);
        
        // Set splash as the MainWindow.
        // This prevents app from exiting with no main window.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = splashWindow;
        }
        
        // Show the splash (it's now the main window).
        splashWindow.Show();
        
        // Create progress reporter for splash.
        var progress = new Progress<(double percent, string message)>(report =>
        {
            // Update splash window progress on UI thread.
            splashWindow.UpdateProgress(report.percent, report.message);
        });
        
        // Load all game resources.
        try
        {
            await LoadGameResources(progress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Phantasma] Error during resource loading: {ex.Message}");
            throw;
        }
        
        // Create the real game window.
        var gameWindow = new MainWindow
        {
            DataContext = new MainWindowBinder(),
        };
        
        // Swap MainWindow to the game window.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop2)
        {
            desktop2.MainWindow = gameWindow;
        }
        
        // Close splash; show game window.
        gameWindow.Show();
        splashWindow.Close();
    }
    
    /// <summary>
    /// Show the main game window after initialization is complete.
    /// </summary>
    private void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowBinder(),
            };
            desktop.MainWindow.Show();
        }
    }

    /// <summary>
    /// Get the appropriate splash image path based on screen dimensions.
    /// </summary>
    private string GetSplashImagePath()
    {
        string dims = configuration.GetValueOrDefault("screen-dims", "640x480");
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
            Path.Combine(configuration.GetValueOrDefault("include-dirname", ""), baseName),
            baseName
        };
        
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        
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
            await Task.Run(() => InitializeKernel());
            
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
            await Task.Run(() => CreateMainSession());
            
            // Final initialization.
            progress.Report((95, "Starting Phantasma..."));
            await Task.Delay(500); // Small delay for effect
            
            progress.Report((100, "Complete!"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during game loading: {ex.Message}");
            throw;
        }
    }

    // ===================================================================
    // RESOURCE LOADING
    // ===================================================================
    
    private void InitializeConfig()
    {
        // Set default configuration values.
        if (!configuration.ContainsKey("screen-dims"))
            configuration["screen-dims"] = "1024x768";
        
        if (!configuration.ContainsKey("sound-enabled"))
            configuration["sound-enabled"] = "yes";
    }

    private void InitializeKernel()
    {
        // Initialize Kernel (which contains IronScheme).
        try
        {
            kernel = new Kernel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize Kernel: {ex.Message}");
            Console.WriteLine("Continuing without Scheme support...");
        }
    }

    private void LoadGameData()
    {
        Console.WriteLine("Loading game data...");

        // Initialize game directory from the load filename (static method).
        string? schemeFile = InitializeGameDirectory(loadFileName);

        if (schemeFile == null)
        {
            Console.WriteLine("[Phantasma] Cannot continue without a valid game.");
            return;
        }

        // Now update instance configuration with game-specific values.
        configuration["include-dirname"] = GameDataDirectory;

        if (!configuration.ContainsKey("saved-games-dirname"))
        {
            configuration["saved-games-dirname"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Phantasma",
                "Saves",
                CurrentGame);
        }
        Directory.CreateDirectory(configuration["saved-games-dirname"]);

        // Load the game.
        Console.WriteLine($"Loading world from: {schemeFile}");
        LoadSchemeFile(schemeFile);
    }

    private void LoadGraphics()
    {
        // TODO: Load sprite sheets and tiles
    }

    private void LoadSounds()
    {
        if (configuration.GetValueOrDefault("sound-enabled", "yes") == "yes")
        {
            // TODO: Load sound files
        }
    }

    private void LoadMaps()
    {
        // TODO: Load map data
    }

    private void LoadScripts()
    {
        if (!string.IsNullOrEmpty(loadFileName))
        {
            // TODO: Load save game
        }
        else
        {
            // TODO: Load default game
        }
    }

    // ===================================================================
    // COMMAND-LINE PARSING
    // ===================================================================

    private static void PrintVersion()
    {
        Console.Write("{0} {1}\r\n", _programName, _packageVersion);
        Console.Write("Ported from Nazghul, Copyright (C) 2003 Gordon McNutt & Sam Glasby\r\n"+
                      "to .NET in 2025 by Nathan Papke.\r\n"+
                      "{0} comes with NO WARRANTY,\r\n"+
                      "to the extent permitted by law.\r\n"+
                      "You may redistribute copies of {0}\r\n"+
                      "under the terms of the GNU General Public License.\r\n"+
                      "For more information about these matters,\r\n"+
                      "see the file named COPYING.\r\n",
            _programName);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  {0} [options] <load-file>", _programName);
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
        
        common.TickMilliseconds = Common.MS_PER_TICK;
        common.AnimationTicks = Common.ANIMATION_TICKS;

        while (c < args.Length && args[c].Substring(0, 1) == "-")
        {
            switch (args[c].Substring(1, 1))
            {
                case "t":
                    common.TickMilliseconds = int.Parse(args[c].Substring(3));
                    break;
                case "a":
                    common.AnimationTicks = int.Parse(args[c].Substring(3));
                    break;
                case "s":
                    configuration["sound-enabled"] = int.Parse(args[c].Substring(3)) != 0 ? "yes" : "no";
                    break;
                case "T":
                    common.ShowAllTerrain = 1;
                    break;
                case "d":
                    developerMode = 1;
                    //DEBUG = 1;
                    //VERBOSE = 1;
                break;
                case "f":
                    fullScreenMode = 1;
                    break;
                case "R":
                    // Set the filename for recording keystrokes.
                    configuration["record-filename"] = args[c].Substring(3);
                    break;
                case "S":
                    // Set the speed to play back recorded keystrokes.
                    configuration["playback-speed"] = args[c].Substring(3);
                    break;
                case "P":
                    // Set the file to play back keystrokes from.
                    configuration["playback-filename"] = args[c].Substring(3);
                    break;
                case "I":
                    // Set the directory for read-only game and cfg files.
                    configuration["include-dirname"] = args[c].Substring(3);
                    break;
                case "G":
                    // Set the directory for read-write game and cfg files.
                    configuration["saved-games-dirname"] = args[c].Substring(3);
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
                    printOptions = 1;
                    break;
                case "r":
                    // Set the screen dimensions.
                    configuration.Add("screen-dims", args[c].Substring(3));
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
            loadFileName = args[c];
    }

    // ===================================================================
    // UTILITY METHODS
    // ===================================================================

    /// <summary>
    /// Check if developer mode is enabled.
    /// </summary>
    public bool IsDeveloperMode()
    {
        return developerMode == 1;
    }

    /// <summary>
    /// Check if fullscreen mode is requested.
    /// </summary>
    public bool IsFullScreenMode()
    {
        return fullScreenMode == 1;
    }

    /// <summary>
    /// Resolve a path relative to the game data directory.
    /// This is the primary method - all asset loading should use this.
    /// </summary>
    /// <param name="relativePath">Path as used in scripts (e.g., "sprites/humans.png")</param>
    /// <returns>Full absolute path</returns>
    public static string ResolvePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return relativePath;

        // If already absolute, return as-is.
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        // Normalize path separators.
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Resolve relative to game data directory.
        return Path.Combine(GameDataDirectory, relativePath);
    }
    
    /// <summary>
    /// Initialize the game data directory from a filename or game name.
    /// 
    /// Supports multiple formats:
    ///   "Haxima"           → Games/Haxima/haxima.scm
    ///   "haxima.scm"       → Search: ./haxima.scm, Games/haxima.scm, Games/haxima/haxima.scm
    ///   "Games/Haxima"     → Games/Haxima/haxima.scm
    ///   "/full/path/game"  → /full/path/game/game.scm
    /// </summary>
    /// <param name="input">Game name, script filename, or path</param>
    /// <returns>Full path to the main script file, or null if not found</returns>
    private static string? InitializeGameDirectory(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("[Phantasma] Error: No game specified.");
            Console.WriteLine("[Phantasma] Usage: Phantasma.exe <game-name> or <script.scm>");
            Console.WriteLine("[Phantasma] Examples:");
            Console.WriteLine("[Phantasma]   Phantasma.exe MyGame");
            Console.WriteLine("[Phantasma]   Phantasma.exe mygame.scm");
            return null;
        }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string? scriptPath = null;
        string? gameDir = null;
        string? gameName = null;

        // Check if input has .scm extension.
        bool hasSchemeExtension = input.EndsWith(".scm", StringComparison.OrdinalIgnoreCase);

        if (hasSchemeExtension)
        {
            // Input is a script filename (e.g., "haxima.scm").
            string filename = input;
            string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

            // Search locations in order:
            string[] searchPaths =
            {
                // 1. Same directory as Phantasma.exe
                Path.Combine(baseDir, filename),
                // 2. Games/ subdirectory
                Path.Combine(GamesDirectory, filename),
                // 3. Games/{name}/ subdirectory
                Path.Combine(GamesDirectory, nameWithoutExt, filename)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    scriptPath = Path.GetFullPath(path);
                    gameDir = Path.GetDirectoryName(scriptPath);
                    gameName = nameWithoutExt;
                    break;
                }
            }

            if (scriptPath == null)
            {
                Console.WriteLine($"[Phantasma] Error: Script not found: {filename}");
                Console.WriteLine("[Phantasma] Searched:");
                foreach (var path in searchPaths)
                {
                    Console.WriteLine($"[Phantasma]   {path}");
                }
                return null;
            }
        }
        else
        {
            // Input is a game/directory name (e.g., "Haxima" or "Games/Haxima").
            string dirPath;
            
            if (Path.IsPathRooted(input))
            {
                // Absolute path.
                dirPath = input;
                gameName = Path.GetFileName(input);
            }
            else if (input.StartsWith("Games" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                     input.StartsWith("Games/", StringComparison.OrdinalIgnoreCase))
            {
                // Already includes Games/ prefix.
                dirPath = Path.Combine(baseDir, input);
                gameName = Path.GetFileName(input);
            }
            else
            {
                // Just game name - look in Games/ directory.
                dirPath = Path.Combine(GamesDirectory, input);
                gameName = input;
            }

            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine($"[Phantasma] Error: Game directory not found: {dirPath}");
                return null;
            }

            gameDir = Path.GetFullPath(dirPath);

            // Look for matching .scm file.
            string[] candidates =
            {
                Path.Combine(gameDir, gameName.ToLower() + ".scm"),
                Path.Combine(gameDir, gameName + ".scm"),
                Path.Combine(gameDir, "main.scm"),
                Path.Combine(gameDir, "game.scm")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    scriptPath = candidate;
                    break;
                }
            }

            if (scriptPath == null)
            {
                Console.WriteLine($"[Phantasma] Error: No main script found in {gameDir}");
                Console.WriteLine("[Phantasma] Expected one of:");
                foreach (var candidate in candidates)
                {
                    Console.WriteLine($"[Phantasma]   {Path.GetFileName(candidate)}");
                }
                return null;
            }
        }

        // Set the static properties (no configuration access here).
        GameDataDirectory = gameDir!;
        CurrentGame = gameName!;

        Console.WriteLine($"[Phantasma] Game: {CurrentGame}");
        Console.WriteLine($"[Phantasma] Directory: {GameDataDirectory}");
        Console.WriteLine($"[Phantasma] Script: {scriptPath}");

        return scriptPath;
    }
}
