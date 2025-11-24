using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using IronScheme;
using IronScheme.Compiler;
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
    /// <summary>
    /// Initialize the Scheme interpreter and register all kern-* API functions.
    /// </summary>
    public Kernel()
    {
        try
        {
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
            $"(begin\n {schemeCode}\n)".Eval();
        
            var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"{elapsedMs:F0} ms to load {filename}");
        }
        catch (SchemeException schemeEx)
        {
            // Extract detailed Scheme error information.
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("SCHEME ERROR:");
            Console.WriteLine(schemeEx.ToString()
                .Replace("\n", "\r\n").Replace("\r\r", "\r"));
        
            if (schemeEx.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {schemeEx.InnerException.Message
                    .Replace("\n", "\r\n").Replace("\r\r", "\r")}");
            }
    
            // The real issue might be in the Data property.
            if (schemeEx.Data != null && schemeEx.Data.Count > 0)
            {
                foreach (var key in schemeEx.Data.Keys)
                {
                    Console.WriteLine($"{key}: {schemeEx.Data[key]}");
                }
            }
    
            // Also check the stack trace.
            Console.WriteLine($"Stack: {schemeEx.StackTrace}");
        
            // Try to get the actual Scheme error details.
            Console.WriteLine($"ToString: {schemeEx.ToString()
                .Replace("\n", "\r\n").Replace("\r\r", "\r")}");
            Console.WriteLine("═══════════════════════════════════════════");
        
            throw new Exception($"Scheme error in {filename}: {schemeEx.Message
                .Replace("\n", "\r\n").Replace("\r\r", "\r")}", schemeEx);
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
        // Basic R6RS boot—ensures core libs (rnrs base, etc.) are loaded without extensions.
        //"(set! newline (lambda () (display \"\\r\\n\")))".Eval();
        "(import (rnrs) (ironscheme))".Eval();
        "(display \"Core R6RS booted.\\r\\n\")".Eval();
    
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
    private void DefineFunction(string schemeName, Delegate csharpMethod)
    {
        var closure = Closure.Create(csharpMethod, -1);
        $"(define {schemeName} {{0}})".Eval(closure);
    }
    
    // =============================================================================
    // PUBLIC STATIC C# METHODS - Called from Scheme via clr-static-call
    // =============================================================================
    
    /// <summary>
    /// (kern-mk-sprite tag filename transparent-color)
    /// Creates a sprite from an image file.
    /// </summary>
    public static object MakeSprite(object tag, object spriteSet, object nFrames, object index, object wave, object facings)
    {
        dynamic ss = spriteSet;
        
        // Calculate source coordinates from index
        int idx = Convert.ToInt32(index ?? 0);
        int cols = ss.Cols;
        int tileWidth = ss.Width;
        int tileHeight = ss.Height;
        
        // Calculate grid position
        int col = idx % cols;
        int row = idx / cols;
        
        var sprite = new Sprite
        {
            Tag = ss.Filename?.ToString(),  // The image filename from sprite set
            SourceX = col * tileWidth + ss.OffsetX,  // Calculate X position in sprite sheet
            SourceY = row * tileHeight + ss.OffsetY, // Calculate Y position in sprite sheet
            WPix = tileWidth,
            HPix = tileHeight,
            NFrames = Convert.ToInt32(nFrames ?? 1)
        };
        
        return sprite;
    }
    
    public static object MakeSpriteSet(object tag, object width, object height, object rows, object cols, object offx, object offy, object filename) 
    {
        // For now, just store the metadata in a dictionary or anonymous object
        // The actual sprite loading would happen elsewhere
        var spriteSetData = new 
        {
            Tag = tag?.ToString(),
            Width = Convert.ToInt32(width ?? 32),
            Height = Convert.ToInt32(height ?? 32),
            Rows = Convert.ToInt32(rows ?? 1),
            Cols = Convert.ToInt32(cols ?? 1),
            OffsetX = Convert.ToInt32(offx ?? 0),
            OffsetY = Convert.ToInt32(offy ?? 0),
            Filename = filename?.ToString()
        };
        
        // Return the metadata - MakeSprite will use this
        return spriteSetData;
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
    public static object MakeTerrain(object tag, object name, object pclass, object sprite, object alpha, object light)
    {
        var terrain = new Terrain
        {
            Name = name?.ToString(),
            PassabilityClass = pclass == null ? 0 : (int)Convert.ToDouble(pclass),
            Sprite = sprite as Sprite,
            Alpha = Convert.ToByte(alpha),
            Light = Convert.ToInt32(light)
        };
        
        Console.WriteLine($"  Created terrain: {tag} '{terrain.Name}' (pclass={terrain.PassabilityClass})");
        
        return terrain;
    }
    
    /// <summary>
    /// (kern-mk-place tag name wrapping? wilderness? width height [terrain-fill])
    /// Creates a place (map).
    /// </summary>
    public static object MakePlace(object tag, object name, object arg3, object arg4, object width, object height)
    {
        var place = new Place
        {
            Name = name?.ToString(),
            Width = Convert.ToInt32(width ?? 128),
            Height = Convert.ToInt32(height ?? 128)
        };
        
        // Initialize the terrain grid
        place.TerrainGrid = new Terrain[place.Width, place.Height];
        
        return place;
    }
    
    public static object MakeCharacter(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeObject(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeObjectType(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeSpecies(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeOccupation(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeParty(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object MakeSound(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // KERN-SET API IMPLEMENTATIONS
    // These set global session properties.
    // ===================================================================
    
    public static object SetCrosshair(object args)
    { 
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object SetCursor(object args)
    { 
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object SetFrame(object args)
    { 
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object SetAscii(object args)
    { 
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object SetClock(object args)
    { 
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // KERN-PLACE API IMPLEMENTATIONS
    // ===================================================================
    
    public static object PlaceGetWidth(object place)
    {
        var p = place as Place;
        return p?.Width ?? 0;
    }
    
    public static object PlaceGetHeight(object place)
    {
        var p = place as Place;
        return p?.Height ?? 0;
    }
    
    public static object PlaceSetTerrain(object place, object x, object y, object terrain)
    {
        var p = place as Place;
        var t = terrain as Terrain;
        
        if (p != null && t != null)
        {
            int xPos = Convert.ToInt32(x ?? 0);
            int yPos = Convert.ToInt32(y ?? 0);
            
            if (xPos >= 0 && xPos < p.Width && yPos >= 0 && yPos < p.Height)
            {
                p.TerrainGrid[xPos, yPos] = t;
                // Only log occasionally to avoid spam
                if (xPos % 10 == 0 && yPos % 10 == 0)
                    Console.WriteLine($"  Set terrain at ({xPos},{yPos})");
            }
        }
        
        return Builtins.Unspecified;
    }
    
    public static object PlaceGetTerrain(object place, object x, object y)
    {
        var p = place as Place;
        if (p != null)
        {
            int xPos = Convert.ToInt32(x ?? 0);
            int yPos = Convert.ToInt32(y ?? 0);
        
            if (xPos >= 0 && xPos < p.Width && yPos >= 0 && yPos < p.Height)
            {
                var terrain = p.TerrainGrid[xPos, yPos];
                return terrain ?? Builtins.Unspecified;
            }
        }
    
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // KERN-OBJ API IMPLEMENTATIONS
    // ===================================================================
    
    public static object ObjectPutAt(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    public static object ObjectGetLocation(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // MISC API IMPLEMENTATIONS
    // ===================================================================
    
    /// <summary>
    /// (kern-print string)
    /// Prints a message to the console.
    /// </summary>
    public static object Print(object args)
    {
        // Handle both direct calls and cons lists.
        string message = args?.ToString() ?? "(null)";
    
        if (args is Cons cons)
        {
            message = cons.car?.ToString() ?? "(null)";
        }
    
        Console.WriteLine($"[Scheme] {message}");
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-include filename)
    /// Loads another Scheme file.
    /// </summary>
    public static object Include(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
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
