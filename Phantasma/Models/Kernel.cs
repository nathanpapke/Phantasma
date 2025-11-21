using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using IronScheme;
using IronScheme.Hosting;
using IronScheme.Runtime;
using IronScheme.Scripting;
using Microsoft.CSharp.RuntimeBinder;

namespace Phantasma.Models;

/// <summary>
/// The Kernel manages the Scheme interpreter and provides the kern-* API functions
/// that Scheme scripts can call to interact with the game engine.
/// 
/// This mirrors Nazghul's kern.c, which initializes TinyScheme and registers
/// all the kern-* functions as callable from Scheme code.
/// 
/// Architecture:
/// 1. Initialize IronScheme interpreter
/// 2. Register all kern-* functions as Scheme callables
/// 3. Load .scm files which call kern-* functions
/// 4. Kern functions create C# game objects and modify game state
/// </summary>
public class Kernel
{
    private object schemeEnvironment;
    
    // Define the Procedure delegate type that IronScheme expects.
    public delegate object Procedure(object args);
    
    // Static registry to prevent GC of our functions
    private static readonly Dictionary<string, object> FunctionRegistry = new Dictionary<string, object>();
    
    /// <summary>
    /// Initialize the Scheme interpreter and register all kern-* API functions.
    /// </summary>
    public Kernel()
    {
        // Initialize IronScheme.
        try
        {
            // Create the Scheme environment.
            schemeEnvironment = "(interaction-environment)".Eval();
            
            // Register all kern-* API functions.
            RegisterKernelApi();
            
            Console.WriteLine("Kernel initialized successfully.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize Scheme interpreter: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Load and execute a Scheme file.
    /// </summary>
    public void LoadSchemeFile(string filename)
    {
        if (schemeEnvironment == null)
        {
            Console.WriteLine("Scheme not available - skipping file load.");
            return;
        }
    
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"Scheme file not found: {filename}");
        }
    
        try
        {
            Console.WriteLine($"Loading Scheme file: {filename}");
            var startTime = DateTime.Now;
        
            string schemeCode = File.ReadAllText(filename);
        
            // Evaluate the Scheme code.
            schemeCode.Eval();
        
            var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"{elapsedMs:F0} ms to load {filename}");
        }
        catch (IronScheme.Runtime.SchemeException schemeEx)
        {
            // Extract detailed Scheme error information.
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("SCHEME ERROR:");
            Console.WriteLine($"Message: {schemeEx.Message}");
        
            if (schemeEx.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {schemeEx.InnerException.Message}");
            }
        
            // Try to get the actual Scheme error details.
            Console.WriteLine($"ToString: {schemeEx.ToString()}");
            Console.WriteLine("═══════════════════════════════════════════");
        
            throw new Exception($"Scheme error in {filename}: {schemeEx.Message}", schemeEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("GENERAL ERROR:");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("═══════════════════════════════════════════");
        
            throw new Exception($"Error loading {filename}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Register all kern-* functions with the Scheme interpreter.
    /// Each registration makes a C# method callable from Scheme.
    /// </summary>
    private void RegisterKernelApi()
    {
        try
        {
            // Import (rnrs) - R6RS standard library.
            // TODO: Figure out the correct way to import required libraries.
            Console.WriteLine("Importing (rnrs) (ironscheme)...");
            "(import (rnrs) (ironscheme))".Eval();
            Console.WriteLine("  * (rnrs) (ironscheme) imported");
        
            // Import (ironscheme) - IronScheme base library.
            //Console.WriteLine("Importing (ironscheme clr)...");
            //"(import (ironscheme clr))".Eval();
            //Console.WriteLine("  * (ironscheme clr) imported");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to import libraries: {ex.Message}");
            throw;
        }
    
        // Now define each kern-* function.
        Console.WriteLine("Registering kern-* functions...");
        
        // ===================================================================
        // KERN-MK API - Object Creation Functions
        // ===================================================================
        
        DefineFunction("kern-mk-sprite", MakeSprite);
        DefineFunction("kern-mk-sprite-set", MakeSpriteSet);
        DefineFunction("kern-mk-terrain", MakeTerrain);
        DefineFunction("kern-mk-place", MakePlace);
        DefineFunction("kern-mk-char", MakeCharacter);
        DefineFunction("kern-mk-obj", MakeObject);
        DefineFunction("kern-mk-obj-type", MakeObjectType);
        DefineFunction("kern-mk-species", MakeSpecies);
        DefineFunction("kern-mk-occ", MakeOccupation);
        DefineFunction("kern-mk-party", MakeParty);
        DefineFunction("kern-mk-sound", MakeSound);
        
        // ===================================================================
        // KERN-SET API - Session Configuration Functions
        // ===================================================================
        
        DefineFunction("kern-set-crosshair", SetCrosshair);
        DefineFunction("kern-set-cursor", SetCursor);
        DefineFunction("kern-set-frame", SetFrame);
        DefineFunction("kern-set-ascii", SetAscii);
        DefineFunction("kern-set-clock", SetClock);
        
        // ===================================================================
        // KERN-PLACE API - Map/Place Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-place-get-width", PlaceGetWidth);
        DefineFunction("kern-place-get-height", PlaceGetHeight);
        DefineFunction("kern-place-set-terrain", PlaceSetTerrain);
        DefineFunction("kern-place-get-terrain", PlaceGetTerrain);
        
        // ===================================================================
        // KERN-OBJ API - Object Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-obj-put-at", ObjectPutAt);
        DefineFunction("kern-obj-get-location", ObjectGetLocation);
        
        // ===================================================================
        // MISC API - Utility Functions
        // ===================================================================
        
        DefineFunction("kern-print", Print);
        DefineFunction("kern-include", Include);
        
        // TODO: Add remaining kern-* functions as needed.
        // The full Nazghul kern.c has ~150 functions.
        // We'll add them incrementally as we need them.
    }
    
    /// <summary>
    /// Helps to define a Scheme function that calls a C# delegate.
    /// </summary>
    private void DefineFunction(string schemeName, Func<object[], object> csharpFunc)
    {
        // TODO: Provide hooks for the private static C# methods to be called in Scheme.
    }
    
    // =============================================================================
    // PUBLIC STATIC C# METHODS - Called from Scheme via clr-static-call
    // =============================================================================
    
    /// <summary>
    /// (kern-mk-sprite tag filename transparent-color)
    /// Creates a sprite from an image file.
    /// </summary>
    public static object MakeSprite(object[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("[ERROR] kern-mk-sprite: need 3 arguments (tag filename trans-color)");
            return null;
        }
        
        string tag = args[0]?.ToString() ?? "";
        string filename = args[1]?.ToString() ?? "";
        int transColor = Convert.ToInt32(args[2]);
        
        var sprite = new Sprite(filename);
        Phantasma.RegisterObject(tag, sprite);
        
        Console.WriteLine($"Created sprite: {tag} -> {filename}");
        return sprite;
    }
    
    public static object MakeSpriteSet(object[] args) 
    {
        if (args.Length < 8)
        {
            Console.WriteLine("[ERROR] kern-mk-sprite-set: need 8 arguments (tag filename trans-color)");
            return null;
        }
        
        string tag = args[0] as string;
        int tileWidth = Convert.ToInt32(args[1]);
        int tileHeight = Convert.ToInt32(args[2]);
        int rows = Convert.ToInt32(args[3]);
        int cols = Convert.ToInt32(args[4]);
        int offsetX = Convert.ToInt32(args[5]);
        int offsetY = Convert.ToInt32(args[6]);
        string filename = args[7] as string;
        
        // Load the sprite sheet image.
        Bitmap image = Models.SpriteManager.LoadImage(filename);
        
        if (image == null)
        {
            Console.WriteLine("[ERROR] kern-mk-sprite-set {tag}: could not load {filename}");
            return null;
        }
        
        // Create array of sprites, one for each tile in the grid.
        int totalTiles = rows * cols;
        var sprites = new Models.Sprite[totalTiles];
        
        for (int index = 0; index < totalTiles; index++)
        {
            int row = index / cols;
            int col = index % cols;
            
            int sourceX = offsetX + (col * tileWidth);
            int sourceY = offsetY + (row * tileHeight);
            
            // Create sprite the same shared SourceImage.
            sprites[index] = Models.Sprite.CreateStatic(
                $"{tag}_{index}",  // Temporary tag
                image,             // Shared SourceImage
                sourceX,           // SourceX for this tile
                sourceY,           // SourceY for this tile
                tileWidth,         // WPix
                tileHeight         // HPix
            );
        }
        
        // Register the array so kern-mk-sprite can access it.
        Phantasma.RegisterObject(tag, sprites);
        
        Console.WriteLine($"Created sprite set: {tag} from {filename} ({cols}x{rows} = {totalTiles} tiles of {tileWidth}x{tileHeight})");
        return sprites;
    }
    
    /// <summary>
    /// (kern-mk-terrain tag name pclass sprite alpha light [effect-proc])
    /// Creates a terrain type.
    /// 
    /// Parameters:
    /// - tag: Symbol or string identifier (e.g., 't_grass)
    /// - name: Display name for the terrain
    /// - pclass: Passability class integer (see PassabilityTable constants)
    /// - sprite: Sprite object for rendering
    /// - alpha: Alpha transparency (0-255, 255 = fully opaque)
    /// - light: Light level emitted by terrain (0 = no light)
    /// - effect-proc: Optional procedure called when stepping on terrain
    /// </summary>
    public static object MakeTerrain(object[] args)
    {
        // Validate argument count.
        if (args.Length < 6)
        {
            Console.WriteLine("[ERROR] kern-mk-terrain: need at least 6 arguments (tag name pclass sprite alpha light [effect-proc])");
            return null;
        }
        
        // Extract tag (handle both symbol 't_grass and string "t_grass").
        string tag = args[0]?.ToString() ?? "";
        // Remove leading quote if it's a symbol.
        if (tag.StartsWith("'"))
            tag = tag.Substring(1);
        
        // Extract name.
        string name = args[1]?.ToString() ?? "";
        
        // Extract pclass - can be either integer or symbol.
        int pclass;
        if (args[2] is int intPclass)
        {
            pclass = intPclass;
        }
        else
        {
            // Handle symbol like 'p_land.
            string pclassStr = args[2]?.ToString() ?? "";
            pclass = ParsePassabilitySymbol(pclassStr);
        }
        
        // Extract sprite object.
        var sprite = args[3] as Sprite;
        if (sprite == null)
        {
            Console.WriteLine($"[ERROR] kern-mk-terrain {tag}: sprite argument must be a Sprite object");
            return null;
        }
        
        // Extract alpha (0-255).
        int alpha = Convert.ToInt32(args[4]);
        if (alpha < 0 || alpha > 255)
        {
            Console.WriteLine($"[WARNING] kern-mk-terrain {tag}: alpha {alpha} out of range (0-255), clamping");
            alpha = Math.Clamp(alpha, 0, 255);
        }
        
        // Extract light level.
        int light = Convert.ToInt32(args[5]);
        
        // Create terrain object.
        var terrain = new Terrain
        {
            Name = name,
            Sprite = sprite,
            PassabilityClass = pclass,
            Alpha = (byte)alpha,
            Light = light,
            Transparent = (alpha < 255) // If not fully opaque, it's transparent.
        };
        
        // Handle optional effect procedure (args[6]).
        if (args.Length > 6 && args[6] != null)
        {
            // TODO: Store the effect procedure
            // This would be a Scheme closure that gets called when 
            // a character steps on this terrain.
            // For now, just log that we received it.
            Console.WriteLine($"[INFO] kern-mk-terrain {tag}: effect procedure provided (not yet implemented)");
        }
        
        // Register the terrain in the global object registry.
        Phantasma.RegisterObject(tag, terrain);
        
        Console.WriteLine($"Created terrain: {tag} ({name}) pclass={pclass} alpha={alpha} light={light}");
        
        return terrain;
    }
    
    /// <summary>
    /// (kern-mk-place tag name wrapping? wilderness? width height [terrain-fill])
    /// Creates a place (map).
    /// </summary>
    public static object MakePlace(object[] args)
    {
        if (args.Length < 6)
        {
            Console.WriteLine("[ERROR] kern-mk-place: need at least 6 arguments");
            return null;
        }
        
        string tag = args[0]?.ToString() ?? "";
        string name = args[1]?.ToString() ?? "";
        bool wrapping = Convert.ToBoolean(args[2]);
        bool wilderness = Convert.ToBoolean(args[3]);
        int width = Convert.ToInt32(args[4]);
        int height = Convert.ToInt32(args[5]);
        
        var place = new Place(width, height, name, wrapping, wilderness);
        
        // Optional terrain fill (args[6]).
        if (args.Length > 6)
        {
            var fillTerrain = args[6] as Terrain;
            if (fillTerrain != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        place.SetTerrain(x, y, fillTerrain);
                    }
                }
            }
        }
        
        Phantasma.RegisterObject(tag, place);
        
        Console.WriteLine($"Created place: {tag} ({name}) {width}x{height}");
        return place;
    }
    
    public static object MakeCharacter(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeObject(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeObjectType(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeSpecies(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeOccupation(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeParty(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object MakeSound(object[] args)
    {
        // TODO: Implement
        return null; 
    }
    
    // ===================================================================
    // KERN-SET API IMPLEMENTATIONS
    // These set global session properties.
    // ===================================================================
    
    public static object SetCrosshair(object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    public static object SetCursor(object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    public static object SetFrame(object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    public static object SetAscii(object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    public static object SetClock(object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    // ===================================================================
    // KERN-PLACE API IMPLEMENTATIONS
    // ===================================================================
    
    public static object PlaceGetWidth(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[ERROR] kern-place-get-width: need place argument.");
            return 0;
        }
        
        var place = args[0] as Place;
        return place?.Width ?? 0;
    }
    
    public static object PlaceGetHeight(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[ERROR] kern-place-get-height: need place argument");
            return 0;
        }
        
        var place = args[0] as Place;
        return place?.Height ?? 0;
    }
    
    public static object PlaceSetTerrain(object[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("[ERROR] kern-place-set-terrain: need 4 arguments");
            return null;
        }
        
        var place = args[0] as Place;
        int x = Convert.ToInt32(args[1]);
        int y = Convert.ToInt32(args[2]);
        var terrain = args[3] as Terrain;
        
        if (place == null || terrain == null)
        {
            Console.WriteLine("[ERROR] kern-place-set-terrain: invalid arguments");
            return null;
        }
        
        place.SetTerrain(x, y, terrain);
        return null;
    }
    
    public static object PlaceGetTerrain(object[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("[ERROR] kern-place-get-terrain: need 3 arguments");
            return null;
        }
        
        var place = args[0] as Place;
        int x = Convert.ToInt32(args[1]);
        int y = Convert.ToInt32(args[2]);
        
        if (place == null)
        {
            Console.WriteLine("[ERROR] kern-place-get-terrain: invalid place");
            return null;
        }
        
        return place.GetTerrain(x, y);
    }
    
    // ===================================================================
    // KERN-OBJ API IMPLEMENTATIONS
    // ===================================================================
    
    public static object ObjectPutAt(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    public static object ObjectGetLocation(object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    // ===================================================================
    // MISC API IMPLEMENTATIONS
    // ===================================================================
    
    /// <summary>
    /// (kern-print string)
    /// Prints a message to the console.
    /// </summary>
    public static object Print(object[] args)
    {
        if (args.Length < 1) return null;
        
        string message = args[0]?.ToString() ?? "";
        Console.WriteLine($"[Scheme] {message}");
        return null;
    }
    
    /// <summary>
    /// (kern-include filename)
    /// Loads another Scheme file.
    /// </summary>
    public static object Include(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[ERROR] kern-include: need filename argument");
            return null;
        }
        
        string filename = args[0]?.ToString() ?? "";
        
        // We need to call LoadSchemeFile, but this is a static method.
        // For now, just evaluate the file using IronScheme's load.
        $"(load \"{filename}\")".Eval();
        
        return null;
    }
    
    // ===================================================================
    // ERROR HANDLING
    // ===================================================================
    
    public static void LoadError(string message)
    {
        Console.WriteLine($"[LOAD ERROR] {message}");
        // TODO: Track load errors like Nazghul does
    }
    
    public static void RuntimeError(string message)
    {
        Console.WriteLine($"[RUNTIME ERROR] {message}");
        // TODO: Track runtime errors
    }
    
    // =============================================================================
    // HELPER METHODS
    // =============================================================================

    /// <summary>
    /// Parse passability class symbols like 'p_land, 'p_water, etc.
    /// These symbols map to the passability class constants defined in PassabilityTable.
    /// </summary>
    private static int ParsePassabilitySymbol(string symbol)
    {
        // Remove leading quote if present (from Scheme symbols)
        symbol = symbol.TrimStart('\'');
    
        // Map Nazghul passability symbols to our integer constants
        switch (symbol)
        {
            case "p_none":
                return PassabilityTable.PCLASS_NONE;  // 0
            case "p_land":
                return 1;  // PC_GRASS - normal land terrain
            case "p_road":
                return 2;  // PC_ROAD - roads, paths
            case "p_forest":
                return 3;  // PC_FOREST - trees
            case "p_hills":
                return 4;  // PC_HILLS - hills, rough terrain
            case "p_shallow":
                return 5;  // PC_SHALLOW - shallow water
            case "p_shoals":
                return 5;  // Alias for shallow
            case "p_water":
                return 6;  // PC_WATER - normal water
            case "p_deep":
                return 7;  // PC_DEEP_WATER - deep water
            case "p_mountain":
                return 8;  // PC_MOUNTAIN - impassable mountains
            case "p_wall":
                return 9;  // PC_WALL - walls, buildings
            case "p_repel":
                return 9;  // Alias for wall (blocks movement)
            case "p_lava":
                return 10;  // PC_LAVA - lava
            case "p_swamp":
                return 11;  // PC_SWAMP - swamps, marshes
            case "p_fire":
                return 12;  // PC_FIRE - fire fields
            case "p_ice":
                return 13;  // PC_ICE - ice
            case "p_air":
                return 14;  // For flying creatures
            default:
                Console.WriteLine($"[WARNING] Unknown passability class '{symbol}', defaulting to PCLASS_NONE");
                return PassabilityTable.PCLASS_NONE;
        }
    }
}
