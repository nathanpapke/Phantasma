using System;
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
    
    public static object SetCrosshair(object objTypeRef)
    {
        ObjectType objType = null;
    
        // Try direct cast first
        if (objTypeRef is ObjectType ot)
        {
            objType = ot;
        }
        // Try looking up by string tag
        else if (objTypeRef is string tag)
        {
            objType = Phantasma.GetRegisteredObject(tag) as ObjectType;
        }
    
        if (objType == null)
        {
            Console.WriteLine("kern-set-crosshair: invalid object type");
            return "#f".Eval();
        }
    
        // Store with well-known key "crosshair"
        Phantasma.RegisterObject("crosshair", objType);
    
        Console.WriteLine($"Crosshair type set to: {objType.Name}");
    
        return "#t".Eval();
    }
    
    /// <summary>
    /// (kern-set-cursor sprite)
    /// Set the text input cursor sprite for the command window.
    /// This is the blinking cursor when typing, NOT the targeting crosshair.
    /// </summary>
    public static object SetCursor(object spriteRef)
    {
        // Get the sprite from the registry.
        Sprite sprite = null;
    
        if (spriteRef is Sprite spr)
        {
            sprite = spr;
        }
        else if (spriteRef is string tag)
        {
            sprite = Phantasma.GetRegisteredObject(tag) as Sprite;
        }
    
        if (sprite == null)
        {
            Console.WriteLine("kern-set-cursor: invalid sprite");
            return "#f".Eval();
        }
    
        return "#t".Eval();
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
    public static object SetClock(object yearObj, object monthObj, object weekObj, 
        object dayObj, object hourObj, object minObj)
    {
        int year = Convert.ToInt32(yearObj ?? 0);
        int month = Convert.ToInt32(monthObj ?? 0);
        int week = Convert.ToInt32(weekObj ?? 0);
        int day = Convert.ToInt32(dayObj ?? 0);
        int hour = Convert.ToInt32(hourObj ?? 0);
        int min = Convert.ToInt32(minObj ?? 0);
    
        var session = Phantasma.MainSession;
        if (session == null)
        {
            Console.WriteLine("[SetClock] Warning: No main session.");
            return Builtins.Unspecified;
        }
    
        session.Clock.Set(year, month, week, day, hour, min);
    
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-set-time-accel multiplier)
    /// Sets how fast time passes (1 = normal, 2 = double speed, etc.).
    /// Used for camping/resting.
    /// </summary>
    /// <param name="accelObj"></param>
    /// <returns></returns>
    public static object SetTimeAcceleration(object accelObj)
    {
        int accel = Convert.ToInt32(accelObj ?? 1);
        accel = Math.Max(1, accel);  // Minimum 1x speed
    
        var session = Phantasma.MainSession;
        if (session == null)
        {
            Console.WriteLine("[SetTimeAccel] Warning: No main session.");
            return Builtins.Unspecified;
        }
    
        session.TimeAcceleration = accel;
        Console.WriteLine($"[SetTimeAccel] Time acceleration set to {accel}x.");
    
        return Builtins.Unspecified;
    }

    /// <summary>
    /// (kern-set-spell-words (word1 word2 word3 ...))
    /// Set spell syllables/words for Ultima-style magic.
    /// </summary>
    public static object SetSpellWords(object words)
    {
        var wordsVector = Builtins.ListToVector(words);
        if (wordsVector is object[] wordsArray)
        {
            for (int i = 0; i < wordsArray.Length && i < 26; i++)
            {
                string word = wordsArray[i]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(word))
                {
                    char letter = (char)('A' + i);
                    Magic.AddWordGlobal(letter, word);
                }
            }

            Console.WriteLine($"  Set {wordsArray.Length} spell words (global)");
        }

        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-set-wind direction duration)
    /// Sets wind direction and duration.
    /// </summary>
    /// <param name="dirObj"></param>
    /// <param name="durObj"></param>
    /// <returns></returns>
    public static object SetWind(object dirObj, object durObj)
    {
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
    public static object SetCampingProc(object args)
    {
        try
        {
            // Extract the procedure from args.
            object proc = ExtractFirstArg(args);
            
            if (proc == null)
            {
                Console.Error.WriteLine("[kern-set-camping-proc] Error: bad args");
                return Builtins.Unspecified;
            }
            
            // Store in session.
            var session = Phantasma.MainSession;
            if (session != null)
            {
                session.SetCampingProc(proc);
            }
            else
            {
                //Phantasma.PendingCampingProc = proc;
            }
            
            Console.WriteLine("[kern-set-camping-proc] Camping procedure set");
            
            // Return the proc (Nazghul returns the proc pointer).
            return proc;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[kern-set-camping-proc] Error: {ex.Message}");
            return Builtins.Unspecified;
        }
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
    public static object SetStartProc(object args)
    {
        try
        {
            // Extract the procedure from args.
            object proc = ExtractFirstArg(args);
            
            if (proc == null)
            {
                Console.Error.WriteLine("[kern-set-start-proc] Error: bad args");
                return Builtins.Unspecified;
            }
            
            // Store in session.
            var session = Phantasma.MainSession;
            if (session != null)
            {
                session.SetStartProc(proc);
            }
            else
            {
                //Phantasma.PendingStartProc = proc;
            }
            
            Console.WriteLine("[kern-set-start-proc] Start procedure set");
            
            // Return the proc (Nazghul returns the proc pointer).
            return proc;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[kern-set-start-proc] Error: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
}
