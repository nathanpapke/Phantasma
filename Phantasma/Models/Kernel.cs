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
public partial class Kernel
{
    public const string KEY_CURRENT_PLACE = "current-place";
    public const string KEY_PLAYER_CHARACTER = "player-character";
    public const string KEY_PLAYER_PARTY = "player-party";
    
    
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
        
        string dataDir = Phantasma.Configuration.GetValueOrDefault("include-dirname", "Data");
        string nazPath = Path.Combine(dataDir, "naz.scm");
        
        if (File.Exists(nazPath))
        {
            Console.WriteLine($"Loading standard definitions: {nazPath}");
            LoadSchemeFile(nazPath);
        }
        else
        {
            Console.WriteLine($"Note: naz.scm not found at {nazPath} - using minimal defaults");
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
        "(import (rnrs) (ironscheme))".Eval();
        "(display \"Core R6RS booted.\\r\\n\")".Eval();
        //"(define (set-global! sym val) (eval `(define ,sym ',val) (interaction-environment)))".Eval();

    
        // Now define each kern-* function.
        Console.WriteLine("Registering kern-* functions...");
        
        // ===================================================================
        // KERN-MK API - Object Creation Functions
        // ===================================================================
        
        DefineFunction("kern-mk-sprite", MakeSprite);
        DefineFunction("kern-mk-sprite-set", MakeSpriteSet);
        DefineFunction("kern-mk-terrain", MakeTerrain);
        DefineFunction("kern-mk-terrain-type", MakeTerrainType);
        DefineFunction("kern-mk-palette", MakePalette);
        DefineFunction("kern-mk-map", MakeMap);
        DefineFunction("kern-mk-place", MakePlace);
        DefineFunction("kern-mk-mmode", MakeMovementMode);
        DefineFunction("kern-mk-species", MakeSpecies);
        DefineFunction("kern-mk-occ", MakeOccupation);
        DefineFunction("kern-mk-char", MakeCharacter);
        DefineFunction("kern-mk-obj", MakeObject);
        DefineFunction("kern-mk-obj-type", MakeObjectType);
        DefineFunction("kern-mk-arms-type", MakeArmsType);
        DefineFunction("kern-mk-container", MakeContainer);
        DefineFunction("kern-mk-party", MakeParty);
        DefineFunction("kern-mk-player", MakePlayer);
        DefineFunction("kern-mk-reagent-type", MakeReagentType);
        DefineFunction("kern-mk-spell", MakeSpell);
        DefineFunction("kern-mk-sound", MakeSound);
        
        // ===================================================================
        // KERN-PARTY API - Party Functions
        // ===================================================================
        
        DefineFunction("kern-party-add-member", PartyAddMember);
        DefineFunction("kern-party-set-wandering", PartySetWandering);
        
        // ===================================================================
        // KERN-SET API - Session Configuration Functions
        // ===================================================================
        
        DefineFunction("kern-set-crosshair", SetCrosshair);
        DefineFunction("kern-set-cursor", SetCursor);
        DefineFunction("kern-set-frame", SetFrame);
        DefineFunction("kern-set-ascii", SetAscii);
        DefineFunction("kern-set-clock", SetClock);
        DefineFunction("kern-set-time-accel", SetTimeAcceleration);
        DefineFunction("kern-set-spell-words", SetSpellWords);
        
        // ===================================================================
        // KERN-ADD API - Status Effect Functions
        // ===================================================================
        
        DefineFunction("kern-add-reveal", AddReveal);
        DefineFunction("kern-add-quicken", AddQuicken);
        DefineFunction("kern-add-time-stop", AddTimeStop);
        DefineFunction("kern-add-magic-negated", AddMagicNegated);
        DefineFunction("kern-add-xray-vision", AddXrayVision);
        
        // ===================================================================
        // KERN-GET API - Accessor Functions
        // ===================================================================
        
        DefineFunction("kern-get-player", GetPlayer);
        
        // ===================================================================
        // KERN-PLACE API - Map/Place Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-place-get-width", PlaceGetWidth);
        DefineFunction("kern-place-get-height", PlaceGetHeight);
        DefineFunction("kern-place-set-terrain", PlaceSetTerrain);
        DefineFunction("kern-place-get-terrain", PlaceGetTerrain);
        DefineFunction("kern-place-set-current", PlaceSetCurrent);
        
        // ===================================================================
        // KERN-OBJ API - Object Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-obj-put-at", ObjectPutAt);
        DefineFunction("kern-obj-get-name", ObjectGetName);
        DefineFunction("kern-obj-get-location", ObjectGetLocation);
        DefineFunction("kern-obj-get-conversation", ObjectGetConversation);
        
        // ===================================================================
        // KERN-CHAR API - Character Functions
        // ===================================================================
        
        DefineFunction("kern-char-get-hp", CharacterGetHp);
        DefineFunction("kern-char-get-max-hp", CharacterGetMaxHp);
        DefineFunction("kern-char-get-level", CharacterGetLevel);
        DefineFunction("kern-char-add-spell", CharacterAddSpell);
        DefineFunction("kern-char-knows-spell", CharacterKnowsSpell);
        DefineFunction("kern-cast-spell", CastSpell);
        
        // ===================================================================
        // KERN-CONV API - Conversation Functions
        // ===================================================================
        
        DefineFunction("kern-conv-say", ConversationSay);
        DefineFunction("kern-conv-get-reply", ConversationGetReply);
        DefineFunction("kern-conv-get-yes-no", ConversationGetYesNo);
        DefineFunction("kern-conv-get-amount", ConversationGetAmount);
        DefineFunction("kern-conv-trade", ConversationTrade);
        DefineFunction("kern-conv-end", ConversationEnd);
        
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
    
    // Most public static C# methods implementing kern-* functions are called
    // from other C# files.
    
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
    public static object Include(object filename)
    {
        try
        {
            string path = filename?.ToString();
            if (string.IsNullOrEmpty(path))
            {
                RuntimeError("kern-include: null or empty filename");
                return "#f".Eval();
            }
            
            // Use Phantasma's LoadSchemeFile which handles paths correctly.
            Phantasma.LoadSchemeFile(path);
            
            Console.WriteLine($"[kern-include] Loaded: {path}");
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-include: {ex.Message}");
            return "#f".Eval();
        }
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
    
    /// <summary>
    /// Get a default color for terrain based on its tag or name.
    /// This ensures every terrain has a color for fallback rendering.
    /// </summary>
    private static string GetTerrainColor(string tag, string name)
    {
        // Convert to lowercase for matching.
        string key = (tag + name).ToLower();
        
        // Common terrain colors based on Nazghul conventions
        if (key.Contains("grass") || key.Contains("field")) return "#228B22";  // Forest green
        if (key.Contains("water") || key.Contains("shallow") || key.Contains("deep")) return "#1E90FF";  // Dodger blue
        if (key.Contains("tree") || key.Contains("forest")) return "#006400";  // Dark green
        if (key.Contains("mountain") || key.Contains("hill")) return "#8B7355";  // Burlywood
        if (key.Contains("stone") || key.Contains("cobble")) return "#696969";  // Dim gray
        if (key.Contains("dirt") || key.Contains("path")) return "#8B4513";  // Saddle brown
        if (key.Contains("sand") || key.Contains("desert")) return "#F4A460";  // Sandy brown
        if (key.Contains("snow") || key.Contains("ice")) return "#FFFAFA";  // Snow
        if (key.Contains("lava") || key.Contains("fire")) return "#FF4500";  // Orange red
        if (key.Contains("swamp") || key.Contains("marsh")) return "#556B2F";  // Dark olive green
        if (key.Contains("bridge")) return "#A0522D";  // Sienna
        if (key.Contains("floor") || key.Contains("dungeon")) return "#404040";  // Dark gray
        if (key.Contains("wall")) return "#2F4F4F";  // Dark slate gray
        if (key.Contains("door")) return "#8B4513";  // Saddle brown
        if (key.Contains("void") || key.Contains("boundary")) return "#000000";  // Black
        
        // Default fallback
        return "#808080";  // Gray
    }
    
    /// <summary>
    /// Helper to unpack IronScheme Cons list into a regular List.
    /// </summary>
    private static List<object> UnpackArgs(object args)
    {
        var result = new List<object>();
    
        if (args == null)
            return result;
        
        // If it's already a single value (not a list), return it as a single-element list.
        if (args is not Cons)
        {
            result.Add(args);
            return result;
        }
    
        // Traverse the Cons list.
        var current = args as Cons;
        while (current != null)
        {
            result.Add(current.car);
        
            if (current.cdr is Cons next)
                current = next;
            else
            {
                // cdr might be the last element or null.
                if (current.cdr != null && !(current.cdr is bool b && !b))
                    result.Add(current.cdr);
                break;
            }
        }
    
        return result;
    }
    
    /// <summary>
    /// Convert various Scheme boolean representations to C# bool.
    /// </summary>
    private static bool ConvertToBool(object value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is string s) return s.ToLower() == "#t" || s.ToLower() == "true";
        // IronScheme uses specific boolean objects
        return value.ToString() != "#f" && value.ToString() != "False";
    }
}
