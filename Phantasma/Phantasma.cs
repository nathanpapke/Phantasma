using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    
    private static Phantasma _instance;
    
    public static Phantasma Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("Phantasma not initialized. Call Initialize() first.");
            }
            return _instance;
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

    public static Dictionary<string, string> Configuration => _instance.configuration;
    public static Common Common => _instance.common;
    public static Dimensions Dimensions => _instance.dimensions;
    public static Kernel Kernel => _instance.kernel;
    public static CombatSounds CombatSounds => combatSounds;
    
    // ===================================================================
    // STATIC CONVENIENCE METHODS (Avoid typing .Instance everywhere)
    // ===================================================================
    
    public static void RegisterObject(string tag, object obj) 
        => _instance.registerObject(tag, obj);

    public static object GetRegisteredObject(string tag) 
        => _instance.getRegisteredObject(tag);

    public static void LoadSchemeFile(string filename) 
        => _instance.loadSchemeFile(filename);

    public static Session CreateAgentSession() 
        => _instance.createAgentSession();

    public static void DestroyAgentSession(Session session) 
        => _instance.destroyAgentSession(session);
    
    // ===================================================================
    // SESSION MANAGEMENT (Multiple Sessions Allowed)
    // ===================================================================
    
    private Session mainSession;          // The real game
    private List<Session> agentSessions;  // Temporary simulations
    
    public static Session MainSession => _instance.mainSession;
    
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
        if (_instance != null)
        {
            throw new InvalidOperationException("Phantasma already initialized.");
        }
        
        _instance = new Phantasma(args);
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
        Console.WriteLine($"Registered object: {tag} ({obj.GetType().Name})");
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
            Console.WriteLine("Warning: Main session already exists.");
            return;
        }
        
        // Try to get the world objects from Scheme.
        var place = GetRegisteredObject(Kernel.KEY_CURRENT_PLACE) as Place;
        var player = GetRegisteredObject(Kernel.KEY_PLAYER_CHARACTER) as Character;
        
        // Create session with Scheme objects (or nulls, which will use fallback).
        mainSession = new Session(place, player);
        Console.WriteLine("Main session created.");
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
        Console.WriteLine($"Agent session created (total: {agentSessions.Count}).");
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
            Console.WriteLine($"Agent session destroyed (remaining: {agentSessions.Count}).");
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
        Console.WriteLine("All agent sessions destroyed");
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
        var gameWindow = new Views.MainWindow
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
            desktop.MainWindow = new Views.MainWindow
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
            
        if (!configuration.ContainsKey("include-dirname"))
            configuration["include-dirname"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            
        if (!configuration.ContainsKey("saved-games-dirname"))
            configuration["saved-games-dirname"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "Phantasma", 
                "Saves");
            
        if (!configuration.ContainsKey("sound-enabled"))
            configuration["sound-enabled"] = "yes";
            
        // Create directories if they don't exist.
        Directory.CreateDirectory(configuration["include-dirname"]);
        Directory.CreateDirectory(configuration["saved-games-dirname"]);
    }

    private void InitializeKernel()
    {
        // Initialize Kernel (which contains IronScheme).
        Console.WriteLine("Initializing Kernel...");
        
        try
        {
            kernel = new Kernel();
            Console.WriteLine("Kernel initialized successfully.");
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

        // Get the data directory path.
        string dataDir = configuration.GetValueOrDefault("include-dirname", "Data");
    
        // Determine which .scm file to load:
        // 1. If loadFileName is specified on command line, use that
        // 2. Otherwise, default to test-world.scm
        string schemeFile;
    
        if (!string.IsNullOrEmpty(loadFileName))
        {
            // Check if it's an absolute path or relative.
            if (Path.IsPathRooted(loadFileName))
            {
                schemeFile = loadFileName;
            }
            else
            {
                schemeFile = Path.Combine(dataDir, loadFileName);
            }
            Console.WriteLine($"Loading specified file: {schemeFile}");
        }
        else
        {
            schemeFile = Path.Combine(dataDir, "test-world.scm");
            Console.WriteLine($"Loading default: {schemeFile}");
        }

        if (File.Exists(schemeFile))
        {
            Console.WriteLine($"Loading world from: {schemeFile}");
            LoadSchemeFile(schemeFile);
        }
        else
        {
            Console.WriteLine($"Warning: Scheme file not found at {schemeFile}");
            Console.WriteLine("Skipping Scheme world loading.");
        }
    }

    private void LoadGraphics()
    {
        Console.WriteLine("Loading graphics...");
        // TODO: Load sprite sheets and tiles
    }

    private void LoadSounds()
    {
        if (configuration.GetValueOrDefault("sound-enabled", "yes") == "yes")
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
        if (!string.IsNullOrEmpty(loadFileName))
        {
            Console.WriteLine($"Loading save file: {loadFileName}");
            // TODO: Load save game
        }
        else
        {
            Console.WriteLine("Loading default game scripts...");
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
}