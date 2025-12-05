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
        DefineFunction("kern-mk-mmode", MakeMovementMode);
        DefineFunction("kern-mk-species", MakeSpecies);
        DefineFunction("kern-mk-occ", MakeOccupation);
        DefineFunction("kern-mk-char", MakeCharacter);
        DefineFunction("kern-mk-obj", MakeObject);
        DefineFunction("kern-mk-obj-type", MakeObjectType);
        DefineFunction("kern-mk-container", MakeContainer);
        DefineFunction("kern-mk-party", MakeParty);
        DefineFunction("kern-mk-player", MakePlayer);
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
        DefineFunction("kern-obj-get-location", ObjectGetLocation);
        DefineFunction("kern-obj-get-conversation", ObjectGetConversation);
        
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
        
        // Load the sprite sheet image
        string filename = ss.Filename?.ToString();
        var sourceImage = SpriteManager.LoadImage(filename); 
        
        // Calculate source coordinates from index.
        int idx = Convert.ToInt32(index ?? 0);
        int cols = ss.Cols;
        int tileWidth = ss.Width;
        int tileHeight = ss.Height;
        
        // Calculate grid position.
        int col = idx % cols;
        int row = idx / cols;
        
        var sprite = new Sprite
        {
            Tag = tag?.ToString(),  // The image filename from sprite set
            NFrames = Convert.ToInt32(nFrames ?? 1),
            //NTotalFrames
            //Facing
            //Facings
            //Sequence
            //Decor
            WPix = tileWidth,
            HPix = tileHeight,
            //Faded
            //Wave
            SourceImage = sourceImage,
            SourceX = col * tileWidth + ss.OffsetX,  // Calculate X position in sprite sheet
            SourceY = row * tileHeight + ss.OffsetY // Calculate Y position in sprite sheet
            //DisplayChar
        };
            
        // Register with Phantasma for lookup.
        if (tag != null)
        {
            Phantasma.RegisterObject(tag.ToString(), sprite);
        }
        
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
        string tagStr = tag?.ToString() ?? "";
        string nameStr = name?.ToString() ?? "";
        
        var terrain = new Terrain
        {
            Name = name?.ToString(),
            Color = GetTerrainColor(tagStr, nameStr),
            PassabilityClass = pclass == null ? 0 : (int)Convert.ToDouble(pclass),
            //IsPassable
            //MovementCost
            //IsHazardous
            //Effect
            Light = Convert.ToInt32(light),
            Alpha = Convert.ToByte(alpha),
            //Transparent
            //DisplayChar
            Sprite = sprite as Sprite
        };
            
        // Register with Phantasma for lookup.
        if (tag != null)
        {
            Phantasma.RegisterObject(tag.ToString(), terrain);
        }
        
        return terrain;
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
        
        // Initialize the terrain grid.
        place.TerrainGrid = new Terrain[place.Width, place.Height];
            
        // Register with Phantasma for lookup.
        if (tag != null)
        {
            Phantasma.RegisterObject(tag.ToString(), place);
        }
        
        return place;
    }
    
    /// <summary>
    /// (kern-mk-mmode tag name index)
    /// Creates a movement mode.
    /// </summary>
    public static object MakeMovementMode(object tag, object name, object index)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        string nameStr = name?.ToString() ?? "Unknown";
        int indexInt = Convert.ToInt32(index ?? 0);
        
        var mmode = new MovementMode(tagStr, nameStr, indexInt);
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, mmode);
        }
        
        Console.WriteLine($"  Created mmode: {nameStr} (index={indexInt})");
        
        return mmode;
    }
    
    /// <summary>
    /// (kern-mk-species tag name str int dex spd vr mmode 
    ///                  hpmod hpmult mpmod mpmult
    ///                  sleep-sprite weapon visible 
    ///                  damage-sound walking-sound on-death
    ///                  xpval slots spells)
    /// Creates a species definition - full 21-parameter Nazghul signature.
    /// </summary>
    public static object MakeSpecies(
        object tag, object name,
        object str, object intl, object dex, object spd, object vr,
        object mmode,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object sleepSprite, object weapon, object visible,
        object damageSound, object walkingSound, object onDeath,
        object xpval, object slots, object spells)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        // Get Movement Mode
        MovementMode movementMode;
        if (mmode is MovementMode mm)
            movementMode = mm;
        else if (mmode is int i)
            movementMode = new MovementMode(null, "Walking", i);
        else
            movementMode = new MovementMode("mmode-walk", "Walking", 0);
        
        var species = new Species
        {
            Tag = tagStr,
            Name = name?.ToString() ?? "Unknown",
            Str = Convert.ToInt32(str ?? 10),
            Intl = Convert.ToInt32(intl ?? 10),
            Dex = Convert.ToInt32(dex ?? 10),
            Spd = Convert.ToInt32(spd ?? 10),
            Vr = Convert.ToInt32(vr ?? 10),
            MovementMode = movementMode,
            HpMod = Convert.ToInt32(hpmod ?? 10),
            HpMult = Convert.ToInt32(hpmult ?? 5),
            MpMod = Convert.ToInt32(mpmod ?? 5),
            MpMult = Convert.ToInt32(mpmult ?? 2),
            SleepSprite = sleepSprite as Sprite,
            Weapon = weapon as ArmsType,
            Visible = ConvertToBool(visible ?? true),
            // damageSound, walkingSound - TODO when sound system implemented
            // onDeath - TODO when closure system implemented
            XpVal = Convert.ToInt32(xpval ?? 10)
            // slots, spells - TODO: parse lists when needed
        };
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, species);
        }
        
        Console.WriteLine($"  Created species: {species.Name} (str={species.Str}, dex={species.Dex}, hp={species.HpMod}+{species.HpMult}/lvl)");
        
        return species;
    }
    
    /// <summary>
    /// (kern-mk-occ tag name magic hpmod hpmult mpmod mpmult hit def dam arm xpval)
    /// Creates an occupation.
    /// </summary>
    public static object MakeOccupation(
        object tag, object name, object magic,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object hit, object def, object dam, object arm,
        object xpval)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        var occ = new Occupation
        {
            Tag = tagStr,
            Name = name?.ToString() ?? "Unknown",
            Magic = Convert.ToSingle(magic ?? 1.0f),
            HpMod = Convert.ToInt32(hpmod ?? 0),
            HpMult = Convert.ToInt32(hpmult ?? 0),
            MpMod = Convert.ToInt32(mpmod ?? 0),
            MpMult = Convert.ToInt32(mpmult ?? 0),
            HitMod = Convert.ToInt32(hit ?? 0),
            DefMod = Convert.ToInt32(def ?? 0),
            DamMod = Convert.ToInt32(dam ?? 0),
            ArmMod = Convert.ToInt32(arm ?? 0),
            XpVal = Convert.ToInt32(xpval ?? 0)
        };
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, occ);
        }
        
        Console.WriteLine($"  Created occupation: {occ.Name} (magic={occ.Magic:F1}, hp+{occ.HpMod}+{occ.HpMult}/lvl)");
        
        return occ;
    }
    
    /// <summary>
    /// (kern-mk-char tag name species occ sprite base-faction
    ///              str int dex hpmod hpmult mpmod mpmult
    ///              hp xp mp lvl dead
    ///              conv sched ai inventory
    ///              readied-list hooks-list)
    /// Creates a character - full 24-parameter Nazghul signature.
    /// </summary>
    public static object MakeCharacter(
        object tag, object name, object species, object occ, object sprite,
        object baseFaction,
        object str, object intl, object dex,
        object hpmod, object hpmult, object mpmod, object mpmult,
        object hp, object xp, object mp, object lvl,
        object dead,
        object conv, object sched, object ai, object inventory,
        object readiedList, object hooksList)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        var character = new Character();
        character.SetName(name?.ToString() ?? "Unknown");
        
        // Set sprite.
        if (sprite is Sprite s)
            character.CurrentSprite = s;
        
        // Set species if available.
        if (species is Species sp)
            character.Species = sp;
        
        // Set occupation if available.
        if (occ is Occupation o)
            character.Occupation = o;
        
        // Set base faction.
        character.SetBaseFaction(Convert.ToInt32(baseFaction ?? 0));
        
        // Set base stats.
        character.Strength = Convert.ToInt32(str ?? 10);
        character.Intelligence = Convert.ToInt32(intl ?? 10);
        character.Dexterity = Convert.ToInt32(dex ?? 10);
        
        // HP Calculation
        int baseHpMod = Convert.ToInt32(hpmod ?? 10);
        int hpPerLevel = Convert.ToInt32(hpmult ?? 5);
        int currentHp = Convert.ToInt32(hp ?? 0);
        int level = Convert.ToInt32(lvl ?? 1);
        
        character.MaxHP = baseHpMod + (hpPerLevel * level);
        character.HP = currentHp > 0 ? currentHp : character.MaxHP;
        
        // MP Calculation
        int baseMpMod = Convert.ToInt32(mpmod ?? 5);
        int mpPerLevel = Convert.ToInt32(mpmult ?? 2);
        int currentMp = Convert.ToInt32(mp ?? 0);
        
        character.MaxMP = baseMpMod + (mpPerLevel * level);
        character.MP = currentMp > 0 ? currentMp : character.MaxMP;
        
        // XP and Level
        character.Experience = Convert.ToInt32(xp ?? 0);
        character.Level = level;
        
        // Dead Flag
        character.IsDead = ConvertToBool(dead);
        
        // Store conversation closure.
        if (conv != null && !(conv is bool b && b == false))
        {
            character.Conversation = conv;
        }
        // sched - schedule
        // TODO: Implement scheduling system.
        // ai - AI closure
        // TODO: Implement AI system.
        // inventory - Container
        // TODO: Implement inventory.
        
        // readiedList - list of readied arms
        // TODO: Implement equipment.
        // Process readied arms list
        if (readiedList is Cons armsList)   
        {
            // TODO: Iterate through and ready each arm.
            // while (armsList != null) { ... }
        }
        
        // hooksList - list of effect hooks (TODO: implement hooks)
        if (hooksList is Cons hooks)
        {
            // TODO: Process hooks.
        }
        
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, character);
        }
        
        Console.WriteLine($"  Created character: {character.Name} (str={character.Strength}, hp={character.HP}/{character.MaxHP}, lvl={character.Level})");
        
        return character;
    }
    
    /// <summary>
    /// (kern-mk-obj type count)
    /// Creates an object instance from a type.
    /// </summary>
    public static object MakeObject(object type, object count)
    {
        if (type is ObjectType objType)
        {
            var item = new Item
            {
                Type = objType,
                Count = Convert.ToInt32(count ?? 1)
            };
        
            // Copy properties from type.
            item.Name = objType.Name;
            if (objType.Sprite != null)
                item.Type.Sprite = objType.Sprite;
        
            Console.WriteLine($"  Created object: {objType.Name} x{item.Count}");
        
            return item;
        }
    
        Console.WriteLine("  [WARNING] kern-mk-obj: invalid type");
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-mk-obj-type tag name sprite layer gifc-cap gifc)
    /// Creates an object type - Nazghul-compatible 6-parameter signature.
    /// </summary>
    public static object MakeObjectType(
        object tag, object name, object sprite, object layer,
        object capabilities, object interactionHandler)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
    
        var objType = new ObjectType
        {
            Tag = tagStr ?? "unknown",
            Name = name?.ToString() ?? "Unknown",
            Layer = (ObjectLayer)Convert.ToInt32(layer ?? 0),
            Capabilities = Convert.ToInt32(capabilities ?? 0)
        };
    
        if (sprite is Sprite s)
            objType.Sprite = s;
    
        // Store interaction handler closure for later use.
        if (interactionHandler != null && !(interactionHandler is bool b && b == false))
            objType.InteractionHandler = interactionHandler;
    
        if (!string.IsNullOrEmpty(tagStr))
            Phantasma.RegisterObject(tagStr, objType);
    
        Console.WriteLine($"  Created object type: {tagStr} '{objType.Name}' (layer={objType.Layer}, caps={objType.Capabilities})");
    
        return objType;
    }
    
    /// <summary>
    /// (kern-mk-container type trap contents-list)
    /// Creates a container (inventory).
    /// </summary>
    public static object MakeContainer(object type, object trap, object contentsList)
    {
        var container = new Container();
    
        // Type can be nil for player inventory.
        if (type is ObjectType ot)
            container.Type = ot;
    
        // trap - TODO: Implement when closure system ready.
    
        // Contents - List of (Count Type) Pairs
        if (contentsList is Cons contents)
        {
            while (contents != null)
            {
                if (contents.car is Cons entry)
                {
                    int count = Convert.ToInt32(entry.car ?? 1);
                    var rest = entry.cdr as Cons;
                    if (rest?.car is ObjectType itemType)
                    {
                        container.AddItem(new Item(){ Type = itemType, Count = count});
                    }
                }
                contents = contents.cdr as Cons;
            }
        }
    
        Console.WriteLine($"  Created container");
    
        return container;
    }
    
    /// <summary>
    /// (kern-mk-party type faction vehicle)
    /// Creates a party.
    /// </summary>
    public static object MakeParty(object type, object faction, object vehicle)
    {        
        var party = new Party();
        // type - PartyType, ignored for now (TODO: implement PartyType)
        party.Faction = Convert.ToInt32(faction ?? 0);
        // vehicle - Vehicle the party is in, ignored for now
        party.IsPlayerParty = false;
    
        Console.WriteLine($"  Created party (faction={party.Faction})");
    
        return party;
    }
    
    // <summary>
    /// (kern-mk-player tag sprite mv-desc mv-sound food gold ttnm 
    ///                 formation campsite camp-formation vehicle inventory
    ///                 (list members...))
    /// Creates the player party - full 13-parameter Nazghul signature.
    /// </summary>
    public static object MakePlayer(
        object tag, object sprite, object mvDesc, object mvSound,
        object food, object gold, object ttnm,
        object formation, object campsite, object campFormation,
        object vehicle, object inventory,
        object membersList)
    {
        string tagStr = tag?.ToString()?.TrimStart('\'');
        
        // Create the player party.
        var party = new Party();
        party.IsPlayerParty = true;
        party.Faction = 0; // Player faction
        
        // Set sprite if provided.
        if (sprite is Sprite s)
            party.Sprite = s;
        
        // Set movement description.
        party.MovementDescription = mvDesc?.ToString() ?? "walking";
        
        // Set resources
        party.Food = Convert.ToInt32(food ?? 0);
        party.Gold = Convert.ToInt32(gold ?? 0);
        party.TurnsToNextMeal = Convert.ToInt32(ttnm ?? 100);
        
        // TODO: formation, campsite, campFormation - Implement when needed.
        // TODO: mvSound - Implement when sound system ready.
        // TODO: vehicle - Implement when vehicle system ready.
        
        // Set inventory container.
        if (inventory is Container inv)
            party.Inventory = inv;
        
        // Add members from the list.
        Character firstMember = null;
        if (membersList is Cons members)
        {
            while (members != null)
            {
                if (members.car is Character ch)
                {
                    // Check position BEFORE adding to party.
                    var posBefore = ch.GetPosition();
                    Console.WriteLine($"  MakePlayer: {ch.GetName()} position BEFORE AddMember: Place={posBefore?.Place?.Name ?? "NULL"}, X={posBefore?.X}, Y={posBefore?.Y}");
    
                    party.AddMember(ch);
    
                    // Check position AFTER adding to party.
                    var posAfter = ch.GetPosition();
                    Console.WriteLine($"  MakePlayer: {ch.GetName()} position AFTER AddMember: Place={posAfter?.Place?.Name ?? "NULL"}, X={posAfter?.X}, Y={posAfter?.Y}");
    
                    if (firstMember == null)
                        firstMember = ch;
                }
                members = members.cdr as Cons;
            }
        }
        
        // Register the party
        if (!string.IsNullOrEmpty(tagStr))
        {
            Phantasma.RegisterObject(tagStr, party);
        }
        Phantasma.RegisterObject(KEY_PLAYER_PARTY, party);
        
        // Register the first member as the player character
        if (firstMember != null)
        {
            Phantasma.RegisterObject(KEY_PLAYER_CHARACTER, firstMember);
            Console.WriteLine($"  Set player character: {firstMember.GetName()}");
        }
        
        Console.WriteLine($"  Created player party with {party.Size} members (food={party.Food}, gold={party.Gold})");
        
        return party;
    }
    
    public static object MakeSound(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // KERN-PARTY API IMPLEMENTATIONS
    // These set party properties.
    // ===================================================================
    
    /// <summary>
    /// (kern-party-add-member party character)
    /// Adds a character to a party.
    /// </summary>
    public static object PartyAddMember(object party, object character)
    {
        var group = party as Party;
        var member = character as Character;

        return group.AddMember(member);
    }
    
    public static object PartySetWandering(object party, object wandering)
    {
        // kern-party-set-wandering <party> <bool>
        // Sets whether a party wanders randomly.
        
        bool isWandering = wandering is bool b ? b : Convert.ToBoolean(wandering);
        var group = party as Party;
        group.IsWandering = isWandering;
        
        return true;
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
    
    public static object SetClock(object hourObj)
    {
        // (kern-set-clock hour)
        // Sets the game clock to the specified hour (0-23).
        // Time is stored internally as minutes (0-1439).
        
        try
        {
            int hour = Convert.ToInt32(hourObj ?? 0);
            hour = Math.Max(0, Math.Min(23, hour));  // Clamp to 0-23
            
            var session = Phantasma.MainSession;
            if (session != null)
            {
                session.GameClock = hour * 60;  // Convert to minutes
                Console.WriteLine($"[SetClock] Game clock set to {hour}:00");
            }
            else
            {
                Console.WriteLine("[SetClock] Warning: No main session");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-set-clock: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object SetTimeAcceleration(object accelObj)
    {
        // (kern-set-time-accel multiplier)
        // Sets how fast time passes (1 = normal, 2 = double speed, etc.).
        
        try
        {
            int accel = Convert.ToInt32(accelObj ?? 1);
            accel = Math.Max(1, accel);  // Minimum 1x speed
            
            var session = Phantasma.MainSession;
            if (session != null)
            {
                session.TimeAcceleration = accel;
                Console.WriteLine($"[SetTimeAccel] Time acceleration set to {accel}x");
            }
            else
            {
                Console.WriteLine("[SetTimeAccel] Warning: No main session");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-set-time-accel: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    // ===================================================================
    // KERN-ADD API IMPLEMENTATIONS
    // These add status effects to characters.
    // ===================================================================
    
    public static object AddReveal(object characterObj, object durationObj)
    {
        // (kern-add-reveal character duration)
        // Grants ability to see invisible/hidden entities for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.RevealDuration = Math.Max(character.RevealDuration, duration);
                Console.WriteLine($"[AddReveal] {character.GetName()} gained Reveal for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-reveal: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-reveal: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddQuicken(object characterObj, object durationObj)
    {
        // (kern-add-quicken character duration)
        // Grants extra actions per turn for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.QuickenDuration = Math.Max(character.QuickenDuration, duration);
                Console.WriteLine($"[AddQuicken] {character.GetName()} gained Quicken for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-quicken: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-quicken: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddTimeStop(object characterObj, object durationObj)
    {
        // (kern-add-time-stop character duration)
        // Freezes other entities while this character can act for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 5);
            
            if (character != null)
            {
                character.TimeStopDuration = Math.Max(character.TimeStopDuration, duration);
                Console.WriteLine($"[AddTimeStop] {character.GetName()} gained Time Stop for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-time-stop: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-time-stop: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddMagicNegated(object characterObj, object durationObj)
    {
        // (kern-add-magic-negated character duration)
        // Prevents character from casting spells for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.MagicNegatedDuration = Math.Max(character.MagicNegatedDuration, duration);
                Console.WriteLine($"[AddMagicNegated] {character.GetName()} gained Magic Negated for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-magic-negated: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-magic-negated: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddXrayVision(object characterObj, object durationObj)
    {
        // (kern-add-xray-vision character duration)
        // Grants ability to see through walls for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.XrayVisionDuration = Math.Max(character.XrayVisionDuration, duration);
                Console.WriteLine($"[AddXrayVision] {character.GetName()} gained Xray Vision for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-xray-vision: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-xray-vision: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    // ===================================================================
    // KERN-GET API IMPLEMENTATIONS
    // ===================================================================
    
    public static object GetPlayer()
    {
        // (kern-get-player)
        // Returns the player character from the main session.
        
        try
        {
            var session = Phantasma.MainSession;
            if (session != null && session.Player != null)
            {
                return session.Player;
            }
            else
            {
                RuntimeError("kern-get-player: No active session or player");
                return Builtins.Unspecified;
            }
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-get-player: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    /// <summary>
    /// (kern-set-player party)
    /// Sets the party as the player-controlled party.
    /// </summary>
    public static object SetPlayer(object character)
    {
        if (character is Character c)
        {
            Phantasma.RegisterObject(KEY_PLAYER_CHARACTER, c);
            Console.WriteLine($"  Set player: {c.GetName()}");
            return c;
        }
    
        Console.WriteLine($"  WARNING: SetPlayer expected Character, got {character?.GetType().Name}");
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
                // Only log occasionally to avoid spam.
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
    
    /// <summary>
    /// (kern-place-set-current place)
    /// Sets the place as the current game place.
    /// </summary>
    public static object PlaceSetCurrent(object place)
    {
        if (place is Place p)
        {
            Phantasma.RegisterObject(KEY_CURRENT_PLACE, p);
            Console.WriteLine($"Registered current place: {p.Name}");
            return p;
        }
    
        Console.WriteLine("[WARNING] kern-place-set-current: Invalid place object");
        return Builtins.Unspecified;
    }
    
    // ===================================================================
    // KERN-OBJ API IMPLEMENTATIONS
    // ===================================================================
    
    /// <summary>
    /// (kern-obj-put-at obj (place x y))
    /// Places an object at a location. Location is a list.
    /// </summary>
    public static object ObjectPutAt(object obj, object location)
    {
        Console.WriteLine($"  ObjectPutAt called:");
        Console.WriteLine($"    obj type: {obj?.GetType().Name ?? "NULL"}");
        Console.WriteLine($"    location type: {location?.GetType().Name ?? "NULL"}");

        var gameObj = obj as Object;
    
        if (gameObj == null)
        {
            return Builtins.Unspecified;
        }
    
        if (location is Cons locList)
        {
            Console.WriteLine($"    locList.car type: {locList.car?.GetType().Name ?? "NULL"}");
            Console.WriteLine($"    locList.cdr type: {locList.cdr?.GetType().Name ?? "NULL"}");

            var place = locList.car as Place;
            var rest = locList.cdr as Cons;
        
            if (place == null)
            {
                return Builtins.Unspecified;
            }
        
            if (rest != null)
            {
                int x = Convert.ToInt32(rest.car ?? 0);
                var rest2 = rest.cdr as Cons;
                int y = rest2 != null ? Convert.ToInt32(rest2.car ?? 0) : 0;
                
                Console.WriteLine($"    Placing at: {place.Name} ({x}, {y})");
            
                // Verify position BEFORE.
                var posBefore = gameObj.GetPosition();
                Console.WriteLine($"    Position BEFORE: Place={posBefore?.Place?.Name ?? "NULL"}, X={posBefore?.X}, Y={posBefore?.Y}");
            
                place.AddObject(gameObj, x, y);
            
                // Verify position AFTER.
                var posAfter = gameObj.GetPosition();
                Console.WriteLine($"    Position AFTER: Place={posAfter?.Place?.Name ?? "NULL"}, X={posAfter?.X}, Y={posAfter?.Y}");
            }
        }
        else
        {
            Console.WriteLine($"    [ERROR] location is not a Cons!");
        }
    
        return Builtins.Unspecified;
    }
    
    public static object ObjectGetLocation(object args)
    {
        // TODO: Implement
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-obj-get-conversation obj)
    /// Get the conversation closure attached to a character.
    /// </summary>
    public static object ObjectGetConversation(object obj)
    {
        if (obj is Character character)
        {
            return character.Conversation ?? "#f".Eval();
        }

        return "#f".Eval();
    }
    
    // ===================================================================
    // KERN-CONV API IMPLEMENTATIONS
    // Conversation functions for keyword-based dialog.
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-say speaker text)
    /// NPC speaks a line of dialog to the player.
    /// </summary>
    public static object ConversationSay(object speaker, object text)
    {
        string message = text?.ToString() ?? "";
        var session = Phantasma.MainSession;
        session?.LogMessage(message);
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-conv-get-reply pc)
    /// Get a keyword reply from the player.
    /// Returns a symbol representing the keyword (truncated to 4 chars).
    /// </summary>
    public static object ConversationGetReply(object pc)
    {
        // TODO: Implement proper UI input.
        // For now, just return 'bye to end conversation.
        Console.WriteLine("[Conversation] Getting player reply (returning 'bye for now)");
        return "bye".Eval();
    }
    
    /// <summary>
    /// (kern-conv-get-yes-no pc)
    /// Prompt player for yes/no response.
    /// Returns #t for yes, #f for no.
    /// </summary>
    public static object ConversationGetYesNo(object pc)
    {
        // TODO: Implement UI prompt.
        Console.WriteLine("[Conversation] Yes/No prompt (returning #f for now)");
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-conv-get-amount pc)
    /// Prompt player for a numeric amount.
    /// Returns the number entered.
    /// </summary>
    public static object ConversationGetAmount(object pc)
    {
        // TODO: Implement UI prompt.
        Console.WriteLine("[Conversation] Amount prompt (returning 0 for now)");
        return 0;
    }
    
    /// <summary>
    /// (kern-conv-trade npc pc trade-list)
    /// Handle merchant trading interface.
    /// </summary>
    public static object ConversationTrade(object npc, object pc, object tradeList)
    {
        // TODO: Implement trading system.
        Console.WriteLine("[Conversation] Trade interface (not yet implemented)");
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-conv-end)
    /// End the current conversation.
    /// </summary>
    public static object ConversationEnd()
    {
        Console.WriteLine("[Conversation] Ending conversation.");
        Conversation.End();
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
}
