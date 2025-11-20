using System;
using System.Collections.Generic;
using System.IO;
using IronScheme;
using IronScheme.Runtime;
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
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"Scheme file not found: {filename}");
        }
        
        try
        {
            Console.WriteLine($"Loading Scheme file: {filename}");
            var startTime = DateTime.Now;
            
            // Load and evaluate the file.
            string schemeCode = File.ReadAllText(filename);
            schemeCode.Eval();
            
            var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"{elapsedMs:F0} ms to load {filename}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading {filename}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Register all kern-* functions with the Scheme interpreter.
    /// Each registration makes a C# method callable from Scheme.
    /// </summary>
    private void RegisterKernelApi()
    {
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
    private void DefineFunction(string schemeName, Delegate function)
    {
        // In IronScheme, we can define functions using the Callable class.
        $"(define {schemeName} #f)".Eval(); // Create binding
        // TODO: Wire up actual function call - IronScheme API exploration needed.
    }
    
    // ===================================================================
    // KERN-MK API IMPLEMENTATIONS
    // These functions are called from Scheme to create game objects.
    // ===================================================================
    
    /// <summary>
    /// (kern-mk-sprite tag filename transparent-color)
    /// Creates a sprite from an image file.
    /// </summary>
    private object MakeSprite(params object[] args)
    {
        if (args.Length < 3)
        {
            LoadError("kern-mk-sprite: insufficient arguments (need tag, filename, x, y)");
            return null;
        }
        
        string tag = args[0] as string;
        string filename = args[1] as string;
        int x = Convert.ToInt32(args[2]);
        int y = args.Length > 3 ? Convert.ToInt32(args[3]) : 0;

        
        var sprite = new Sprite(filename, x, y);
        Phantasma.Instance.RegisterObject(tag, sprite);
        
        Console.WriteLine($"Created sprite: {tag} from {filename}");
        return sprite;
    }
    
    private object MakeSpriteSet(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    /// <summary>
    /// (kern-mk-terrain tag name pclass sprite alpha light [effect])
    /// Creates a terrain type.
    /// Example: (kern-mk-terrain 't_grass "grass" pclass-grass sprite-grass 0 0)
    /// </summary>
    private object MakeTerrain(params object[] args)
    {
        // Parse arguments: tag, name, pclass, sprite, alpha, light
        if (args.Length < 6)
        {
            LoadError("kern-mk-terrain: insufficient arguments");
            return null;
        }
        
        string tag = args[0] as string;
        string name = args[1] as string;
        int pclass = Convert.ToInt32(args[2]);
        Sprite sprite = args[3] as Sprite;
        int alpha = Convert.ToInt32(args[4]);
        int light = Convert.ToInt32(args[5]);
        
        // Create the terrain.
        var terrain = new Terrain(tag, name, sprite, pclass, alpha, light);
        
        // TODO: Handle optional effect closure (args[6]).
        
        // Register with Phantasma (not a specific session).
        Phantasma.Instance.RegisterObject(tag, terrain);
        
        Console.WriteLine($"Created terrain: {tag} ({name})");
        return terrain;
    }
    
    /// <summary>
    /// (kern-mk-place tag name wrapping? wilderness? width height [terrain-fill])
    /// Creates a place (map).
    /// </summary>
    private object MakePlace(params object[] args)
    {
        if (args.Length < 6)
        {
            LoadError("kern-mk-place: insufficient arguments");
            return null;
        }
        
        string tag = args[0] as string;
        string name = args[1] as string;
        bool wrapping = Convert.ToBoolean(args[2]);
        bool wilderness = Convert.ToBoolean(args[3]);
        int width = Convert.ToInt32(args[4]);
        int height = Convert.ToInt32(args[5]);
        
        var place = new Place(width, height, name, wrapping, wilderness);
        
        // TODO: Handle optional terrain fill (args[6]).
        
        Phantasma.Instance.RegisterObject(tag, place);
        
        Console.WriteLine($"Created place: {tag} ({name}) {width}x{height}");
        return place;
    }
    
    private object MakeCharacter(params object[] args)
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeObject(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeObjectType(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeSpecies(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeOccupation(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeParty(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object MakeSound(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    // ===================================================================
    // KERN-SET API IMPLEMENTATIONS
    // These set global session properties.
    // ===================================================================
    
    private object SetCrosshair(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object SetCursor(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object SetFrame(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object SetAscii(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    private object SetClock(params object[] args) 
    { 
        // TODO: Implement
        return null; 
    }
    
    // ===================================================================
    // KERN-PLACE API IMPLEMENTATIONS
    // ===================================================================
    
    private object PlaceGetWidth(params object[] args)
    {
        if (args.Length < 1)
        {
            RuntimeError("kern-place-get-width: missing place argument");
            return 0;
        }
        
        var place = args[0] as Place;
        if (place == null)
        {
            RuntimeError("kern-place-get-width: invalid place object");
            return 0;
        }
        
        return place.Width;
    }
    
    private object PlaceGetHeight(params object[] args)
    {
        if (args.Length < 1)
        {
            RuntimeError("kern-place-get-height: missing place argument");
            return 0;
        }
        
        var place = args[0] as Place;
        if (place == null)
        {
            RuntimeError("kern-place-get-height: invalid place object");
            return 0;
        }
        
        return place.Height;
    }
    
    private object PlaceSetTerrain(params object[] args)
    {
        if (args.Length < 4)
        {
            RuntimeError("kern-place-set-terrain: insufficient arguments");
            return null;
        }
        
        var place = args[0] as Place;
        int x = Convert.ToInt32(args[1]);
        int y = Convert.ToInt32(args[2]);
        var terrain = args[3] as Terrain;
        
        if (place == null || terrain == null)
        {
            RuntimeError("kern-place-set-terrain: invalid arguments");
            return null;
        }
        
        place.SetTerrain(x, y, terrain);
        return null;
    }
    
    private object PlaceGetTerrain(params object[] args)
    {
        if (args.Length < 3)
        {
            RuntimeError("kern-place-get-terrain: insufficient arguments");
            return null;
        }
        
        var place = args[0] as Place;
        int x = Convert.ToInt32(args[1]);
        int y = Convert.ToInt32(args[2]);
        
        if (place == null)
        {
            RuntimeError("kern-place-get-terrain: invalid place");
            return null;
        }
        
        return place.GetTerrain(x, y);
    }
    
    // ===================================================================
    // KERN-OBJ API IMPLEMENTATIONS
    // ===================================================================
    
    private object ObjectPutAt(params object[] args)
    {
        // TODO: Implement
        return null;
    }
    
    private object ObjectGetLocation(params object[] args)
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
    private object Print(params object[] args)
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
    private object Include(params object[] args)
    {
        if (args.Length < 1)
        {
            LoadError("kern-include: missing filename");
            return null;
        }
        
        string filename = args[0] as string;
        LoadSchemeFile(filename);
        return null;
    }
    
    // ===================================================================
    // ERROR HANDLING
    // ===================================================================
    
    private void LoadError(string message)
    {
        Console.WriteLine($"[LOAD ERROR] {message}");
        // TODO: Track load errors like Nazghul does
    }
    
    private void RuntimeError(string message)
    {
        Console.WriteLine($"[RUNTIME ERROR] {message}");
        // TODO: Track runtime errors
    }
}
