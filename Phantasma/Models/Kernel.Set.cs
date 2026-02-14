using System;
using System.Collections.Generic;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-SET API IMPLEMENTATIONS
    // These set global session properties.
    // ===================================================================
    
    /// <summary>
    /// (kern-set-player party)
    /// Sets the party as the player-controlled party.
    /// </summary>
    public static object SetPlayer(object[] args)
    {
        var character = args.Length > 0 ? args[0] : null;
        
        if (character is Character c)
        {
            Phantasma.RegisterObject(KEY_PLAYER_CHARACTER, c);
            Console.WriteLine($"  Set player: {c.GetName()}");
            return c;
        }
    
        Console.WriteLine($"  WARNING: SetPlayer expected Character, got {character?.GetType().Name}");
        return "nil".Eval();
    }
    
    public static object SetCrosshair(object[] args )
    {
        var objTypeRef = args.Length > 0 ? args[0] : null;
        
        ObjectType? objType = ResolveObject<ObjectType>(objTypeRef);

        if (objType == null)
        {
            return "nil".Eval();
        }

        // Store with well-known "crosshair" key.
        Phantasma.RegisterObject("crosshair", objType);
        
        return objType;
    }
    
    /// <summary>
    /// (kern-set-cursor sprite)
    /// Set the text input cursor sprite for the command window.
    /// This is the blinking cursor when typing, NOT the targeting crosshair.
    /// </summary>
    public static object SetCursor(object[] args)
    {
        var spriteRef = args.Length > 0 ? args[0] : null;
        
        Sprite? sprite = ResolveObject<Sprite>(spriteRef);
        
        if (sprite == null)
        {
            return "nil".Eval();
        }
        
        // Store with well-known "cursor-sprite" key.
        Phantasma.RegisterObject("cursor-sprite", sprite);
        
        return sprite;
    }
    
    public static object SetFrame(object[] args)
    {
        if (args.Length < 13)
        {
            return "nil".Eval();
        }
        
        // Store as anonymous object - access via dynamic later.
        var frameSprites = new
        {
            ULC = ResolveObject<Sprite>(args[0]),   // Upper-left corner
            URC = ResolveObject<Sprite>(args[1]),   // Upper-right corner
            LLC = ResolveObject<Sprite>(args[2]),   // Lower-left corner
            LRC = ResolveObject<Sprite>(args[3]),   // Lower-right corner
            TD = ResolveObject<Sprite>(args[4]),    // T-junction down
            TU = ResolveObject<Sprite>(args[5]),    // T-junction up
            TL = ResolveObject<Sprite>(args[6]),    // T-junction left
            TR = ResolveObject<Sprite>(args[7]),    // T-junction right
            TX = ResolveObject<Sprite>(args[8]),    // Cross junction
            Horz = ResolveObject<Sprite>(args[9]),  // Horizontal edge
            Vert = ResolveObject<Sprite>(args[10]), // Vertical edge
            EndL = ResolveObject<Sprite>(args[11]), // End left
            EndR = ResolveObject<Sprite>(args[12])  // End right
        };
    
        // Register for lookup.
        Phantasma.RegisterObject("frame-sprites", frameSprites);
        
        return "nil".Eval();
    }
    
    public static object SetAscii(object[] args)
    {
        if (args.Length < 2)
        {
            return "nil".Eval();
        }
    
        // Get the sprite set (could be tag string or the object itself).
        object spriteSetRef = args[0];
        int offset = ToInt(args[1], 32);
    
        // Store as anonymous object, same pattern as sprite sets.
        var asciiConfig = new
        {
            SpriteSet = spriteSetRef,
            Offset = offset
        };
    
        // Register for lookup.
        Phantasma.RegisterObject("ascii-config", asciiConfig);
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-set-clock year month week day hour minute)
    /// Sets the game clock to a specific time.
    /// Matches Nazghul's 6-parameter signature exactly.
    /// </summary>
    /// <param name="yearObj"></param>
    /// <param name="monthObj"></param>
    /// <param name="weekObj"></param>
    /// <param name="dayObj"></param>
    /// <param name="hourObj"></param>
    /// <param name="minObj"></param>
    /// <returns></returns>
    public static object SetClock(object[] args)
    {
        if (args.Length < 6)
        {
            return "nil".Eval();
        }
        
        int year = args.Length > 0 ? Convert.ToInt32(args[0]) : 0;
        int month = args.Length > 1 ? Convert.ToInt32(args[1]) : 0;
        int week = args.Length > 2 ? Convert.ToInt32(args[2]) : 0;
        int day = args.Length > 3 ? Convert.ToInt32(args[3]) : 0;
        int hour = args.Length > 4 ? Convert.ToInt32(args[4]) : 0;
        int min = args.Length > 5 ? Convert.ToInt32(args[5]) : 0;
        
        var session = Phantasma.MainSession;
        if (session == null)
        {
            // Session doesn't exist yet - store for later application.
            // This happens when the game data is loaded before the session is created.
            Phantasma.SetPendingClockData(year, month, week, day, hour, min);
            return "nil".Eval();
        }
        
        // Session exists - set the clock directly.
        session.Clock.Set(year, month, week, day, hour, min);
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-set-time-accel multiplier)
    /// Sets how fast time passes (1 = normal, 2 = double speed, etc.).
    /// Used for camping/resting.
    /// </summary>
    /// <param name="accelObj"></param>
    /// <returns></returns>
    public static object SetTimeAcceleration(object[] args)
    {
        var val = args.Length > 0 ? args[0] : null;
        
        int accel = Convert.ToInt32(val ?? 1);
        accel = Math.Max(1, accel);  // Minimum 1x speed
        
        var session = Phantasma.MainSession;
        if (session == null)
        {
            Console.WriteLine("[SetTimeAccel] Warning: No main session.");
            return "nil".Eval();
        }
        
        session.TimeAcceleration = accel;
        
        return "nil".Eval();
    }

    /// <summary>
    /// (kern-set-spell-words (word1 word2 word3 ...))
    /// Set spell syllables/words for Ultima-style magic.
    /// </summary>
    public static object SetSpellWords(object[] args)
    {
        var words = new List<string>();
    
        foreach (var arg in args)
        {
            string word = arg?.ToString()?.Trim('"');
            if (!string.IsNullOrEmpty(word))
                words.Add(word);
        }
    
        // Register for lookup
        Phantasma.RegisterObject("spell-words", words);
    
        // Also set in Magic system for runtime use
        for (int i = 0; i < words.Count && i < 26; i++)
        {
            char letter = (char)('A' + i);
            Magic.AddWordGlobal(letter, words[i]);
        }
        
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-set-wind direction duration)
    /// Sets wind direction and duration.
    /// </summary>
    /// <param name="dirObj"></param>
    /// <param name="durObj"></param>
    /// <returns></returns>
    public static object SetWind(object[] args)
    {
        var dirObj = args.Length > 0 ? args[0] : null;
        var durObj = args.Length > 1 ? args[1] : null;
        
        int direction = Convert.ToInt32(dirObj ?? Common.NORTH);
        int duration = Convert.ToInt32(durObj ?? 0);
        
        var session = Phantasma.MainSession;
        if (session == null)
        {
            Console.WriteLine("[SetWind] Warning: No main session");
            return "#f".Eval();
        }
        
        session.Wind.SetDirection(direction, duration);
        
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-set-camping-proc proc)
    /// Sets the procedure to call each turn when the party is camping in the wilderness.
    /// </summary>
    /// <remarks>
    /// Nazghul kern.c signature:
    ///   KERN_API_CALL(kern_set_camping_proc) - args contains one closure/proc
    ///   session_set_camping_proc(Session, closure_new(sc, proc))
    /// </remarks>
    public static object SetCampingProc(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[kern-set-camping-proc] No procedure provided");
            return "nil".Eval();
        }
        
        // Extract the actual procedure from args[0], not the whole args array!
        var proc = args[0];
        
        if (Phantasma.MainSession == null)
        {
            Phantasma.SetPendingCampingProc(proc);
        }
        else
        {
            Phantasma.MainSession.SetCampingProc(proc);
        }
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-set-start-proc proc)
    /// Sets the procedure to call when the game session starts.
    /// The procedure is called with the player party as an argument.
    /// </summary>
    /// <remarks>
    /// Nazghul kern.c signature:
    ///   KERN_API_CALL(kern_set_start_proc) - args contains one closure/proc
    ///   session_set_start_proc(Session, closure_new(sc, proc))
    ///   
    /// Called by session_run_start_proc():
    ///   closure_exec(session->start_proc, "p", player_party)
    /// </remarks>
    public static object SetStartProc(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[kern-set-start-proc] No procedure provided");
            return "nil".Eval();
        }
        
        // Extract the actual procedure from args[0], not the whole args array!
        object proc = args[0];
        
        if (Phantasma.MainSession == null)
        {
            Phantasma.SetPendingStartProc(proc);
        }
        else
        {
            Phantasma.MainSession.SetStartProc(proc);
        }
        
        return "nil".Eval();
    }
}
