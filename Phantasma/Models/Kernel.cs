using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    
    private static Kernel? instance;

    /// <summary>
    /// The current Kernel instance. Available during initialization.
    /// </summary>
    public static Kernel? Instance => instance;
    
    /// <summary>
    /// Initialize the Scheme interpreter and register all kern-* API functions.
    /// </summary>
    public Kernel()
    {
        // Set instance first so Include can access LoadSchemeFile during init.
        instance = this;
        
        try
        {
            // Register all kern-* API functions.
            RegisterKernelApi();
            RegisterAllFunctions();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize Scheme interpreter: {ex.Message}", ex);
        }
        
        // Load R5RS compatibility layer first.
        LoadCompatibilityLayer();
    }
    
    /// <summary>
    /// Registers all public static Kern* methods as IronScheme Callables.
    /// Call this once at startup before loading any Scheme files.
    /// </summary>
    public static void RegisterAllFunctions()
    {
        var methods = typeof(Kernel).GetMethods(BindingFlags.Public | BindingFlags.Static);
        int count = 0;
    
        foreach (var method in methods)
        {
            // Only register methods that return object and take all object parameters.
            if (method.ReturnType != typeof(object)) continue;
            if (!method.GetParameters().All(p => p.ParameterType == typeof(object))) continue;
        
            // Convert "KernMkSpell" or "MkSpell" to "kern-mk-spell" or "mk-spell".
            var sb = new StringBuilder();
            foreach (char c in method.Name)
            {
                if (char.IsUpper(c))
                {
                    if (sb.Length > 0) sb.Append('-');
                    sb.Append(char.ToLower(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            string schemeName = sb.ToString();
        
            var callable = new CallableMethod(method, schemeName);
            Builtins.SetSymbolValueFast(SymbolTable.StringToObject(schemeName), callable);
            count++;
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
        DefineFunction("kern-fire-missile", FireMissile);
        DefineFunction("kern-mk-reagent-type", MakeReagentType);
        DefineFunction("kern-mk-spell", MakeSpell);
        DefineFunction("kern-mk-effect", MakeEffect);
        DefineFunction("kern-mk-astral-body", MakeAstralBody);
        DefineFunction("kern-mk-vehicle-type", MakeVehicleType);
        DefineFunction("kern-mk-vehicle", MakeVehicle);
        DefineFunction("kern-mk-sound", MakeSound);
        DefineFunction("kern-mk-ptable", MakePassabilityTable);
        DefineFunction("kern-mk-dtable", MakeDiplomacyTable);
        DefineFunction("kern-mk-field-type", MakeFieldType);
        DefineFunction("kern-mk-party-type", MakePartyType);
        DefineFunction("kern-mk-sched", MakeSchedule);
        
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
        DefineFunction("kern-set-wind", SetWind);
        DefineFunction("kern-set-camping-proc", SetCampingProc);
        DefineFunction("kern-set-start-proc", SetStartProc);
        
        // ===================================================================
        // KERN-ADD API - Status Effect Functions
        // ===================================================================
        
        DefineFunction("kern-add-reveal", AddReveal);
        DefineFunction("kern-add-quicken", AddQuicken);
        DefineFunction("kern-add-time-stop", AddTimeStop);
        DefineFunction("kern-add-magic-negated", AddMagicNegated);
        DefineFunction("kern-add-xray-vision", AddXrayVision);
        DefineFunction("kern-add-spell", AddSpell);
        DefineFunction("kern-add-tick-job", AddTickJob);
        
        // ===================================================================
        // KERN-GET API - Accessor Functions
        // ===================================================================
        
        DefineFunction("kern-get-player", GetPlayer);
        DefineFunction("kern-in-los?", InLineOfSight);
        DefineFunction("kern-get-distance", GetDistance);
        DefineFunction("kern-get-objects-at", GetObjectsAt);
        DefineFunction("kern-get-ticks", GetTicks);
        DefineFunction("kern-type-get-gifc", TypeGetGameInterface);
        
        // ===================================================================
        // KERN-PLACE API - Map/Place Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-place-get-width", PlaceGetWidth);
        DefineFunction("kern-place-get-height", PlaceGetHeight);
        DefineFunction("kern-place-set-terrain", PlaceSetTerrain);
        DefineFunction("kern-place-get-terrain", PlaceGetTerrain);
        DefineFunction("kern-place-set-current", PlaceSetCurrent);
        DefineFunction("kern-place-get-name", PlaceGetName);
        DefineFunction("kern-place-get-location", PlaceGetLocation);
        DefineFunction("kern-place-get-neighbor", PlaceGetNeighbor);
        DefineFunction("kern-place-is-wilderness", PlaceIsWilderness);
        DefineFunction("kern-place-is-wrapping", PlaceIsWrapping);
        DefineFunction("kern-place-get-beings", PlaceGetBeings);
        DefineFunction("kern-place-is-passable", PlaceIsPassable);
        DefineFunction("kern-place-is-hazardous", PlaceIsHazardous);
        
        DefineFunction("kern-terrain-set-combat-map", TerrainSetCombatMap);
        
        DefineFunction("kern-place-map", PlaceMap);
        DefineFunction("kern-place-synch", PlaceSynch);
        DefineFunction("kern-place-get-objects", PlaceGetObjects);
        DefineFunction("kern-place-add-subplace", PlaceAddSubplace);
        
        // ===================================================================
        // KERN-OBJ API - Object Manipulation Functions
        // ===================================================================
        
        DefineFunction("kern-obj-put-at", ObjectPutAt);
        DefineFunction("kern-obj-get-name", ObjectGetName);
        DefineFunction("kern-obj-get-type", ObjectGetType);
        DefineFunction("kern-obj-get-location", ObjectGetLocation);
        DefineFunction("kern-obj-get-conversation", ObjectGetConversation);
        DefineFunction("kern-obj-apply-damage", ObjectApplyDamage);
        DefineFunction("kern-obj-add-effect", ObjectAddEffect);
        DefineFunction("kern-obj-remove-effect", ObjectRemoveEffect);
        DefineFunction("kern-obj-has-effect?", ObjectHasEffect);
        DefineFunction("kern-obj-remove", ObjectRemove);
        DefineFunction("kern-obj-relocate", ObjectRelocate);
        DefineFunction("kern-obj-find-path", ObjectFindPath);
        DefineFunction("kern-obj-wander", ObjectWander);
        DefineFunction("kern-obj-is-visible?", ObjectIsVisible);
        DefineFunction("kern-obj-set-visible", ObjectSetVisible);
        DefineFunction("kern-obj-move", ObjectMove);
        DefineFunction("kern-obj-get-ap", ObjectGetActionPoints);
        DefineFunction("kern-obj-set-ap", ObjectSetActionPoints);
        DefineFunction("kern-obj-dec-ap", ObjectDecreaseActionPoints);
        DefineFunction("kern-obj-is-being?", ObjectIsBeing);
        DefineFunction("kern-obj-get-gob", ObjectGetGob);
        DefineFunction("kern-obj-set-gob", ObjectSetGob);
        DefineFunction("kern-obj-set-pclass", ObjectSetPassability);
        DefineFunction("kern-obj-get-sprite", ObjectGetSprite);
        DefineFunction("kern-obj-set-sprite", ObjectSetSprite);
        DefineFunction("kern-obj-set-opacity", ObjectSetOpacity);
        DefineFunction("kern-obj-set-light", ObjectSetLight);
        DefineFunction("kern-obj-get-activity", ObjectGetActivity);
        DefineFunction("kern-obj-set-activity", ObjectSetActivity);
        DefineFunction("kern-obj-heal", ObjectHeal);
        DefineFunction("kern-obj-is-char?", ObjectIsChar);
        
        // ===================================================================
        // KERN-SPECIES API - Species Functions
        // ===================================================================
        
        DefineFunction("kern-species-get-hp-mod", SpeciesGetHpMod);
        DefineFunction("kern-species-get-hp-mult", SpeciesGetHpMult);
        DefineFunction("kern-species-get-mp-mod", SpeciesGetMpMod);
        DefineFunction("kern-species-get-mp-mult", SpeciesGetMpMult);
        
        // ===================================================================
        // KERN-BEING API - Being Functions
        // ===================================================================
        
        DefineFunction("kern-being-pathfind-to", BeingPathfindTo);
        DefineFunction("kern-being-is-hostile?", BeingIsHostile);
        DefineFunction("kern-being-get-visible-hostiles", BeingGetVisibleHostiles);
        DefineFunction("kern-being-set-base-faction", BeingSetBaseFaction);
        DefineFunction("kern-being-get-base-faction", BeingGetBaseFaction);
        DefineFunction("kern-being-get-current-faction", BeingGetCurrentFaction);
        
        // ===================================================================
        // KERN-CHAR API - Character Functions
        // ===================================================================
        
        DefineFunction("kern-char-get-hp", CharacterGetHp);
        DefineFunction("kern-char-get-max-hp", CharacterGetMaxHp);
        DefineFunction("kern-char-get-level", CharacterGetLevel);
        DefineFunction("kern-char-set-hp", CharacterSetHp);
        DefineFunction("kern-char-kill", CharacterKill);
        DefineFunction("kern-char-resurrect", CharacterResurrect);
        DefineFunction("kern-char-set-ai", CharacterSetAI);
        DefineFunction("kern-char-get-mana", CharacterGetMana);
        DefineFunction("kern-char-dec-mana", CharacterDecreaseMana);
        DefineFunction("kern-char-attack", CharacterAttack);
        DefineFunction("kern-char-get-species", CharacterGetSpecies);
        DefineFunction("kern-char-is-asleep?", CharacterIsAsleep);
        DefineFunction("kern-char-set-sleep", CharacterSetSleep);
        
        // ===================================================================
        // KERN-CHAR API - Character Equipment Functions
        // ===================================================================
        
        DefineFunction("kern-char-get-weapons", CharacterGetWeapons);
        DefineFunction("kern-char-arm-self", CharacterArmSelf);
        DefineFunction("kern-char-get-inventory", CharacterGetInventory);
        DefineFunction("kern-char-has-ammo?", CharacterHasAmmo);
        DefineFunction("kern-char-ready", CharacterReady);
        DefineFunction("kern-char-unready", CharacterUnready);
        
        // ===================================================================
        // KERN-OCC API - Occupation Functions
        // ===================================================================
        
        DefineFunction("kern-occ-get-hp-mod", OccupationGetHpMod);
        DefineFunction("kern-occ-get-hp-mult", OccupationGetHpMult);
        DefineFunction("kern-occ-get-mp-mod", OccupationGetMpMod);
        DefineFunction("kern-occ-get-mp-mult", OccupationGetMpMult);
        
        // ===================================================================
        // KERN-DIPLOMACY API - Diplomacy Functions
        // ===================================================================
        
        DefineFunction("kern-dtable-get", DiplomacyTableGet);
        DefineFunction("kern-dtable-set", DiplomacyTableSet);
        DefineFunction("kern-dtable-inc", DiplomacyTableIncrement);
        DefineFunction("kern-dtable-dec", DiplomacyTableDecrement);
        
        // ===================================================================
        // KERN-ARMS API - Arms Functions
        // ===================================================================
        
        DefineFunction("kern-arms-type-get-range", ArmsTypeGetRange);
        DefineFunction("kern-arms-type-get-ammo-type", ArmsTypeGetAmmoType);
        
        // ===================================================================
        // KERN-CONV API - Conversation Functions
        // ===================================================================
        
        DefineFunction("kern-conv-say", ConversationSay);
        DefineFunction("kern-conv-get-reply", ConversationGetReply);
        DefineFunction("kern-conv-get-yes-no?", ConversationGetYesNo);
        DefineFunction("kern-conv-get-amount", ConversationGetAmount);
        DefineFunction("kern-conv-trade", ConversationTrade);
        DefineFunction("kern-conv-end", ConversationEnd);
        
        // ===================================================================
        // KERN-ASTRAL-BODY API
        // ===================================================================
        
        DefineFunction("kern-astral-body-get-phase", AstralBodyGetPhase);
        DefineFunction("kern-astral-body-get-gob", AstralBodyGetGob);
        DefineFunction("kern-astral-body-set-gob", AstralBodySetGob);
        
        // ===================================================================
        // MAP API - Map Functions
        // ===================================================================
        
        DefineFunction("kern-blit-map", BlitMap);
        DefineFunction("kern-map-rotate", MapRotate);
        DefineFunction("kern-map-set-dirty", MapSetDirty);
        
        // ===================================================================
        // MISC API - Utility Functions
        // ===================================================================
        
        DefineFunction("kern-print", Print);
        DefineFunction("kern-include", Include);
        DefineFunction("load", LoadFile);
        DefineFunction("kern-load-file", LoadFile);
        DefineFunction("kern-sound-play", SoundPlay);
        DefineFunction("kern-tag", Tag);
        DefineFunction("kern-dice-roll", DiceRoll);
        DefineFunction("kern-log-msg", LogMessage);
        DefineFunction("kern-fold-rect", FoldRect);
        
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
    /// Registers a variadic function that can be called with 'apply' or
    /// with variable numbers of arguments. Uses CallTargetN signature.
    /// </summary>
    private void DefineFunction(string schemeName, Func<object[], object> method)
    {
        CallTargetN target = args =>
        {
            return method(args);
        };
        var closure = Closure.Create(target, -1);
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
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-include filename)
    /// Registers a file for save game tracking.
    /// Does NOT actually load the file (matches Nazghul behavior).
    /// </summary>
    public static object Include(object args)
    {
        string rawPath = ExtractFilename(args);
    
        if (string.IsNullOrEmpty(rawPath))
        {
            return "nil".Eval();
        }
    
        // Just register for save tracking - don't actually load.
        // TODO: Add Session.RegisterIncludedFile(rawPath) when implementing save/load.
    
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-load-file filename)
    /// Internal function that loads a Scheme file with path resolution and preprocessing.
    /// Called by our overridden 'load' in tinyscheme-compat.scm.
    /// </summary>
    public static object LoadFile(object[] args)
    {
        // Extract first argument.
        var firstArg = args.Length > 0 ? args[0] : null;
        string rawPath = ExtractFilename(firstArg);
        var kernel = Phantasma.Kernel;
    
        if (string.IsNullOrEmpty(rawPath))
        {
            Console.Error.WriteLine("[kern-load-file] Error: could not extract filename");
            return "nil".Eval();
        }
    
        // Redirect Haxima's init.scm to Phantasma's compatible version.
        // Phantasma's init.scm contains the custom 'apply' that handles CallTargetN closures.
        if (rawPath.Equals("init.scm", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith("/init.scm", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith("\\init.scm", StringComparison.OrdinalIgnoreCase))
        {
            string phantasmaInit = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "init.scm");
            if (File.Exists(phantasmaInit))
            {
                Console.WriteLine($"[kern-load-file] Redirecting init.scm to Phantasma version");
                kernel?.LoadSchemeFileInternal(phantasmaInit);
            }
            return "nil".Eval();
        }
    
        string path = Phantasma.ResolvePath(rawPath);
    
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[kern-load-file] File not found: {path}");
            return "nil".Eval();
        }
        
        if (kernel == null)
        {
            Console.Error.WriteLine("[kern-load-file] Error: Kernel is null");
            return "nil".Eval();
        }
    
        kernel.LoadSchemeFile(path);
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-sound-play sound)
    /// Plays a sound at maximum volume.
    /// Equivalent to Nazghul's kern_sound_play() in kern.c lines 3359-3368.
    /// </summary>
    /// <param name="sound">The Sound object to play.</param>
    /// <returns>Unspecified (void in Nazghul).</returns>
    /// <example>
    /// Scheme usage:
    /// (kern-sound-play snd-footstep)
    /// </example>
    public static object SoundPlay(object sound)
    {
        Sound? soundObj = null;
        
        // Handle different input types.
        if (sound is Sound s)
        {
            soundObj = s;
        }
        else if (sound is string tag)
        {
            // Look up by tag.
            soundObj = SoundManager.Instance.GetSound(tag);
            if (soundObj == null)
            {
                // Try registered objects.
                soundObj = Phantasma.GetRegisteredObject(tag) as Sound;
            }
        }
        
        if (soundObj == null)
        {
            // Silent fail.
            return "nil".Eval();
        }
        
        // Play at max volume.
        SoundManager.Instance.Play(soundObj, SoundManager.MaxVolume);
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-tag 'tag object)
    /// Assign a tag to an object and define it in Scheme.
    /// </summary>
    public static object Tag(object[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("[kern-tag] Not enough arguments");
            return null;
        }
        
        string tag = args[0]?.ToString()?.TrimStart('\'') ?? "";
        object obj = args[1];
        
        if (string.IsNullOrEmpty(tag))
        {
            Console.Error.WriteLine("[kern-tag] Invalid tag");
            return null;
        }
        
        // Resolve object if it's a tag string.
        if (obj is string objTag)
        {
            obj = Phantasma.GetRegisteredObject(objTag.TrimStart('\'').Trim('"'));
        }
        
        if (obj == null)
        {
            Console.Error.WriteLine($"[kern-tag] Null object for tag '{tag}'");
            return null;
        }
        
        // Try to set Tag property via reflection (optional - not all objects have it).
        var tagProp = obj.GetType().GetProperty("Tag");
        if (tagProp != null && tagProp.CanWrite && tagProp.PropertyType == typeof(string))
        {
            tagProp.SetValue(obj, tag);
        }
        
        // Register in C# registry.
        if (!string.IsNullOrEmpty(tag))
        {
            Phantasma.RegisterObject(tag, obj);
            $"(define {tag} \"{tag}\")".Eval();
        }
        
        return obj;
    }
    
    public static object DiceRoll(object[] args)
    {
        if (args == null || args.Length < 1)
        {
            Console.WriteLine("[LOAD ERROR] kern-dice-roll: expected 1 arg (dice string)");
            return 0;
        }
        
        string diceExpr = ToTag(args[0]) ?? args[0]?.ToString() ?? "";
        
        int result = Dice.Roll(diceExpr);
        return result;
    }
    
    /// <summary>
    /// (kern-log-msg message)
    /// Logs a message to the game console.
    /// </summary>
    public static object LogMessage(object[] args)
    {
        var sb = new StringBuilder();
        foreach (var arg in args)
            sb.Append(arg?.ToString() ?? "");
        
        Console.WriteLine($"[GAME] {sb.ToString().TrimEnd('\n')}");
        return "nil".Eval();
    }

    /// <summary>
    /// (kern-fold-rect place x y w h proc initial-value)
    /// Folds a procedure over every tile in a rectangle.
    /// For each tile, calls (proc accumulated-value location).
    /// Returns the final accumulated value.
    /// </summary>
    // Note: Nazghul iterates y then x, clipping to map bounds.
    public static object FoldRect(object[] args)
    {
        if (args == null || args.Length < 7)
        {
            Console.WriteLine($"[ERROR] kern-fold-rect: expected 7 args, got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        // Unpack: place, x, y, w, h, proc, initial-value
        object placeArg = args[0];
        int ulcX = Convert.ToInt32(args[1]);
        int ulcY = Convert.ToInt32(args[2]);
        int w    = Convert.ToInt32(args[3]);
        int h    = Convert.ToInt32(args[4]);
        object proc = args[5];
        object val  = args[6];
        
        // Resolve place.
        Place place = placeArg as Place;
        if (place == null && placeArg is string tag)
        {
            place = Phantasma.GetRegisteredObject(tag.TrimStart('\'').Trim('"')) as Place;
        }
        
        if (place == null)
        {
            Console.WriteLine("[ERROR] kern-fold-rect: null place");
            return "nil".Eval();
        }
        
        // Get the callable procedure.
        if (proc is not Callable callable)
        {
            Console.WriteLine($"[ERROR] kern-fold-rect: proc is not callable (got {proc?.GetType().Name ?? "null"})");
            return "nil".Eval();
        }
        
        // Clip rectangle to map bounds.
        int lrcX = Math.Min(place.Width,  ulcX + w);
        int lrcY = Math.Min(place.Height, ulcY + h);
        ulcX = Math.Max(0, ulcX);
        ulcY = Math.Max(0, ulcY);
        
        // Iterate over tiles, calling (proc val loc) for each.
        for (int y = ulcY; y < lrcY; y++)
        {
            for (int x = ulcX; x < lrcX; x++)
            {
                // Build location as Scheme list: (place x y)
                var loc = new Cons(place, new Cons(x, new Cons(y, null)));
                
                try
                {
                    val = callable.Call(val, loc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] kern-fold-rect: callback error at ({x},{y}): {ex.Message}");
                    return val;
                }
            }
        }
        
        return val;
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
    /// Parse a Scheme list of slot values into an int[] array.
    /// Each slot value is a bitmask indicating what type of equipment can go there.
    /// Example: '(slot-weapon slot-weapon slot-body slot-helm)
    /// </summary>
    private static int[]? ParseSlotsList(object slots)
    {
        if (slots == null)
            return null;
        
        var slotList = new List<int>();
        
        // Handle IronScheme Cons (linked list)
        if (slots is Cons cons)
        {
            object? current = cons;
            while (current is Cons c)
            {
                // Get the slot value
                object slotValue = c.car;
                int slotMask = ParseSlotValue(slotValue);
                slotList.Add(slotMask);
                
                current = c.cdr;
            }
        }
        // Handle C# List
        else if (slots is IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable)
            {
                int slotMask = ParseSlotValue(item);
                slotList.Add(slotMask);
            }
        }
        // Handle empty list
        else if (slots.ToString() == "()" || slots.ToString() == "nil")
        {
            return Array.Empty<int>();
        }
        
        return slotList.Count > 0 ? slotList.ToArray() : null;
    }
    
    /// <summary>
    /// Parse a single slot value (symbol or int) into a slot mask.
    /// </summary>
    private static int ParseSlotValue(object value)
    {
        // If it's already an int, use it directly
        if (value is int i)
            return i;
        
        // If it's a symbol or string, look it up
        string? name = value?.ToString()?.ToLowerInvariant();
        
        return name switch
        {
            "slot-weapon" or "weapon" => ArmsType.Slots.Weapon,
            "slot-shield" or "shield" => ArmsType.Slots.Shield,
            "slot-body" or "body" => ArmsType.Slots.Body,
            "slot-helm" or "helm" => ArmsType.Slots.Helm,
            "slot-boots" or "boots" => ArmsType.Slots.Boots,
            "slot-gloves" or "gloves" => ArmsType.Slots.Gloves,
            "slot-amulet" or "amulet" => ArmsType.Slots.Amulet,
            "slot-ring" or "ring" => ArmsType.Slots.Ring,
            "slot-hand" or "hand" => ArmsType.Slots.Weapon,  // Alias
            _ => TryParseInt(value, ArmsType.Slots.Weapon)
        };
    }
    
    /// <summary>
    /// Parse a Scheme list of spell codes into a string[] array.
    /// </summary>
    private static string[]? ParseSpellsList(object spells)
    {
        if (spells == null)
            return null;
        
        var spellList = new List<string>();
        
        // Handle IronScheme Cons (linked list)
        if (spells is Cons cons)
        {
            object? current = cons;
            while (current is Cons c)
            {
                string? spellCode = c.car?.ToString();
                if (!string.IsNullOrEmpty(spellCode))
                    spellList.Add(spellCode);
                
                current = c.cdr;
            }
        }
        // Handle empty list
        else if (spells.ToString() == "()" || spells.ToString() == "nil")
        {
            return Array.Empty<string>();
        }
        
        return spellList.Count > 0 ? spellList.ToArray() : null;
    }
    
    /// <summary>
    /// Try to parse a value as an integer, returning default on failure.
    /// </summary>
    private static int TryParseInt(object? value, int defaultValue)
    {
        if (value == null)
            return defaultValue;
        
        if (value is int i)
            return i;
        
        if (int.TryParse(value.ToString(), out int result))
            return result;
        
        return defaultValue;
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
    
    /// <summary>
    /// Check if a Scheme value is nil/false/#f.
    /// </summary>
    private static bool IsNil(object? value)
    {
        if (value == null) return true;
        if (value is bool b && !b) return true;
        if (value is Cons cons && cons.car == null && cons.cdr == null) return true;
        return false;
    }
    
    /// <summary>
    /// Register slot type symbols for use in Scheme scripts.
    /// Call this during initialization.
    /// </summary>
    public static void RegisterSlotSymbols()
    {
        // Define slot constants in Scheme
        $"(define slot-weapon {ArmsType.Slots.Weapon})".Eval();
        $"(define slot-shield {ArmsType.Slots.Shield})".Eval();
        $"(define slot-body {ArmsType.Slots.Body})".Eval();
        $"(define slot-helm {ArmsType.Slots.Helm})".Eval();
        $"(define slot-boots {ArmsType.Slots.Boots})".Eval();
        $"(define slot-gloves {ArmsType.Slots.Gloves})".Eval();
        $"(define slot-amulet {ArmsType.Slots.Amulet})".Eval();
        $"(define slot-ring {ArmsType.Slots.Ring})".Eval();
        
        // Aliases
        $"(define slot-hand {ArmsType.Slots.Weapon})".Eval();
        
        Console.WriteLine("  Slot type symbols registered");
    }
    
    private static object ListToScheme(List<object> items)
    {
        if (items.Count == 0)
            return "nil".Eval();
        
        // Build list from end to start
        object result = "nil".Eval();
        for (int i = items.Count - 1; i >= 0; i--)
        {
            result = new Cons(items[i], result);
        }
        
        return result;
    }

    /// <summary>
    /// Convert LinkedList of AStarNode to Scheme list of (x y) pairs.
    /// </summary>
    private static object ConvertPathToSchemeList(LinkedList<AStarNode> path)
    {
        var result = new List<object>();

        foreach (var node in path)
        {
            // Create (x y) pair as a Cons.
            var pair = new Cons(node.X, new Cons(node.Y, null));
            result.Add(pair);
        }

        return Cons.FromList(result);
    }
    
    /// <summary>
    /// Convert a Scheme list of lists to a C# List of List of ints.
    /// Used for parsing ptable and dtable arguments.
    /// </summary>
    private static List<List<int>> ConvertToListOfLists(object args)
    {
        var result = new List<List<int>>();
        
        // Handle the outer list (rows).
        object current = args;
        while (current != null && current != "nil".Eval())
        {
            if (current is Cons cons)
            {
                // Get this row.
                var row = ConvertToIntList(cons.car);
                if (row != null)
                {
                    result.Add(row);
                }
                
                // Move to next row.
                current = cons.cdr;
            }
            else
            {
                break;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Convert a Scheme list to a C# List of ints.
    /// Handles integers, floats, and known symbol names.
    /// </summary>
    private static List<int> ConvertToIntList(object listObj)
    {
        var result = new List<int>();

        object current = listObj;
        while (current != null && current != "nil".Eval())
        {
            if (current is Cons cons)
            {
                var val = cons.car;
                int intVal = 0;
                bool converted = false;
                
                // Try direct numeric types first.
                if (val is int i)
                {
                    intVal = i;
                    converted = true;
                }
                else if (val is long l)
                {
                    intVal = (int)l;
                    converted = true;
                }
                else if (val is double d)
                {
                    intVal = (int)d;
                    converted = true;
                }
                // Handle IronScheme symbols.
                else if (val is SymbolId sym)
                {
                    string symName = SymbolTable.IdToString(sym);
                    intVal = ResolvePassabilitySymbol(symName);
                    converted = true;
                    // Only log if it's not a known symbol.
                    if (intVal == 0 && symName != "easy")
                    {
                        Console.WriteLine($"[ConvertToIntList] Resolved symbol '{symName}' -> {intVal}");
                    }
                }
                // Handle string representations of symbols.
                else if (val is string s)
                {
                    intVal = ResolvePassabilitySymbol(s);
                    converted = true;
                }
                // Fallback: try Convert.ToInt32
                else if (val != null)
                {
                    try
                    {
                        intVal = Convert.ToInt32(val);
                        converted = true;
                    }
                    catch
                    {
                        // Try as symbol name.
                        string valStr = val.ToString();
                        intVal = ResolvePassabilitySymbol(valStr);
                        converted = true;
                        Console.Error.WriteLine($"[ConvertToIntList] Converted '{valStr}' -> {intVal}");
                    }
                }
                
                if (converted)
                {
                    result.Add(intVal);
                }
                
                current = cons.cdr;
            }
            else
            {
                break;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Resolve known passability symbol names to their integer values.
    /// These are commonly used in Haxima's game.scm and related files.
    /// </summary>
    private static int ResolvePassabilitySymbol(string symbolName)
    {
        // Remove any leading quote or tick.
        symbolName = symbolName?.TrimStart('\'', '`') ?? "";
        
        return symbolName.ToLowerInvariant() switch
        {
            // Common passability constants
            "norm" => 1,           // Normal movement cost
            "normal" => 1,
            "easy" => 0,           // Free/easy movement
            "cant" => 255,         // Impassable (PTABLE_IMPASSABLE)
            "impassable" => 255,
            "blocked" => 255,
            
            // Numeric passability costs
            "slow" => 2,
            "very-slow" => 3,
            "crawl" => 4,
            
            // Special values
            "water" => 255,        // Usually impassable for walking
            "air" => 0,            // Usually free for flying
            
            // Default: return 0 (passable with no cost penalty)
            _ => 0
        };
    }
    
    /// <summary>
    /// Extract the first argument from a Cons list or return the object itself.
    /// Used for functions that take a single argument.
    /// </summary>
    private static object ExtractFirstArg(object args)
    {
        if (args is Cons cons)
        {
            return cons.car;
        }
        return args;
    }
    
    /// <summary>
    /// Check if an object is an IronScheme SymbolId.
    /// </summary>
    private static bool IsSymbol(object? obj)
    {
        if (obj == null) return false;
        string typeName = obj.GetType().Name;
        return typeName == "SymbolId" || 
               typeName.Contains("Symbol") ||
               obj.GetType().FullName?.Contains("SymbolId") == true;
    }

    /// <summary>
    /// Check if an object is a Scheme pair/list (Cons cell).
    /// </summary>
    private static bool IsPair(object? obj)
    {
        if (obj == null) return false;
        string typeName = obj.GetType().Name;
        return typeName == "Cons" || 
               typeName.Contains("Pair") ||
               obj.GetType().FullName?.Contains("Cons") == true;
    }

    /// <summary>
    /// Safely convert a Scheme object to an integer.
    /// Handles SymbolId, strings, and null values gracefully.
    /// </summary>
    /// <param name="obj">The Scheme object to convert</param>
    /// <param name="defaultValue">Value to return if conversion fails</param>
    /// <returns>The integer value or defaultValue</returns>
    private static int ToInt(object? obj, int defaultValue = 0)
    {
        if (obj == null) return defaultValue;
        
        // Symbols can't be converted to integers
        if (IsSymbol(obj)) return defaultValue;
        
        return (int)Convert.ToDouble(obj);
    }

    /// <summary>
    /// Safely convert a Scheme object to a double.
    /// </summary>
    private static double ToDouble(object? obj, double defaultValue = 0.0)
    {
        if (obj == null) return defaultValue;
        if (IsSymbol(obj)) return defaultValue;
        
        try
        {
            return Convert.ToDouble(obj);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely convert a Scheme object to a byte.
    /// </summary>
    private static byte ToByte(object? obj, byte defaultValue = 0)
    {
        if (obj == null) return defaultValue;
        if (IsSymbol(obj)) return defaultValue;
        
        try
        {
            return Convert.ToByte(obj);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely convert a Scheme object to a boolean.
    /// Handles #t/#f, symbols, and various truthy/falsy values.
    /// </summary>
    private static bool ToBool(object? obj, bool defaultValue = false)
    {
        if (obj == null) return defaultValue;
        
        // Handle IronScheme boolean values
        string str = obj.ToString()?.ToLower() ?? "";
        if (str == "#t" || str == "true" || str == "1") return true;
        if (str == "#f" || str == "false" || str == "0" || str == "nil") return false;
        
        // Symbols that look like true/false
        if (IsSymbol(obj))
        {
            if (str.Contains("true") || str == "t") return true;
            if (str.Contains("false") || str == "f" || str == "nil") return false;
        }
        
        try
        {
            return Convert.ToBoolean(obj);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Convert a Scheme object to a clean string.
    /// Handles both strings and symbols, removing quotes and leading apostrophes.
    /// </summary>
    /// <param name="obj">The Scheme object to convert</param>
    /// <returns>Clean string or null</returns>
    private static string? ToCleanString(object? obj)
    {
        if (obj == null) return null;
        
        string result = obj.ToString() ?? "";
        
        // Remove surrounding quotes
        result = result.Trim('"');
        
        // Remove leading apostrophe from symbols
        if (result.StartsWith("'"))
            result = result.TrimStart('\'');
        
        return result;
    }

    /// <summary>
    /// Extract a tag string from a Scheme symbol or string.
    /// Removes quotes and leading apostrophes.
    /// </summary>
    private static string ToTag(object? obj, string defaultValue = "")
    {
        return ToCleanString(obj) ?? defaultValue;
    }

    /// <summary>
    /// Resolve a registered object from a tag, symbol, or direct reference.
    /// </summary>
    /// <typeparam name="T">Expected type of the registered object</typeparam>
    /// <param name="obj">Tag string, symbol, or direct object reference</param>
    /// <returns>The resolved object or null</returns>
    private static T? ResolveObject<T>(object? obj) where T : class
    {
        if (obj == null) return null;
        
        // Direct reference
        if (obj is T direct) return direct;
        
        // Tag lookup
        string? tag = ToCleanString(obj);
        if (!string.IsNullOrEmpty(tag) && tag != "nil")
        {
            var resolved = Phantasma.GetRegisteredObject(tag);
            if (resolved is T typed) return typed;
        }
        
        return null;
    }
}
