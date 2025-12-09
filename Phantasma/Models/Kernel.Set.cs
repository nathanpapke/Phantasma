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
}
