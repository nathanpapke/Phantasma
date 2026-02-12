using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // (kern-get-player)
    // Returns the player character from the main session.
    public static object GetPlayer()
    {
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
                return "nil".Eval();
            }
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-get-player: {ex.Message}");
            return "nil".Eval();
        }
    }

    /// <summary>
    /// (kern-in-los? place1 x1 y1 place2 x2 y2)
    /// </summary>
    public static object InLineOfSight(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-in-los?] Expected 2 args (loc1 loc2), got {args?.Length ?? 0}");
            return false;
        }
        
        if (!UnpackLocation(args[0], out var place1, out int x1, out int y1))
        {
            Console.WriteLine("[kern-in-los?] Invalid location list 1");
            return false;
        }
        
        if (!UnpackLocation(args[1], out var place2, out int x2, out int y2))
        {
            Console.WriteLine("[kern-in-los?] Invalid location list 2");
            return false;
        }
        
        if (place1 != place2) return false;
        return place1.IsInLineOfSight(x1, y1, x2, y2);
    }

    /// <summary>
    /// (kern-get-distance place1 x1 y1 place2 x2 y2)
    /// </summary>
    public static object GetDistance(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-get-distance] Expected 2 args (loc1 loc2), got {args?.Length ?? 0}");
            return -1;
        }
        
        if (!UnpackLocation(args[0], out var place1, out int x1, out int y1))
        {
            Console.WriteLine("[kern-get-distance] Invalid location list 1");
            return -1;
        }
        
        if (!UnpackLocation(args[1], out var place2, out int x2, out int y2))
        {
            Console.WriteLine("[kern-get-distance] Invalid location list 2");
            return -1;
        }
        
        if (place1 != place2) return -1;
        return place1.GetFlyingDistance(x1, y1, x2, y2);
    }

    /// <summary>
    /// (kern-get-objects-at place x y)
    /// </summary>
    public static object GetObjectsAt(object[] args)
    {
        if (args == null || args.Length < 1)
        {
            Console.WriteLine($"[kern-get-objects-at] Expected 1 arg (loc), got {args?.Length ?? 0}");
            return Cons.FromList(new List<object>());
        }
        
        if (!UnpackLocation(args[0], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-get-objects-at] Invalid location list");
            return Cons.FromList(new List<object>());
        }
        
        var objects = new List<object>(place.GetObjectsAt(x, y));
        return Cons.FromList(objects);
    }
    
    // Implementation
    public static object GetTicks(object args)
    {
        // Return game ticks/turns elapsed.
        return Environment.TickCount;
    }
    
    /// <summary>
    /// (kern-type-get-gifc type)
    /// Gets the game interface closure (gifc) for an object type.
    /// Returns the interaction handler closure, or nil if none.
    /// </summary>
    public static object TypeGetGameInterface(object type)
    {
        // Handle array wrapping.  IronScheme sometimes passes args as Object[] type.
        if (type is object[] arr && arr.Length > 0)
        {
            type = arr[0];
        }
        
        // Handle null - not necessarily an error per Nazghul comment:
        // "Some objects (like characters) have no type"
        if (type == null || IsNil(type))
        {
            return "nil".Eval();
        }
        
        // Try to get ObjectType directly or by tag.
        ObjectType? objType = type as ObjectType;
        
        if (objType == null && type is string typeTag)
        {
            typeTag = type?.ToString()?.TrimStart('\'').Trim('"');
            objType = Phantasma.GetRegisteredObject(typeTag) as ObjectType;
        }
        
        if (objType == null)
        {
            // Not an ObjectType - could be Character, etc. Return nil.
            return "nil".Eval();
        }
    
        if (objType.InteractionHandler != null)
        {
            return objType.InteractionHandler;
        }
        
        // Return the interaction handler (gifc) or nil.
        return objType.InteractionHandler ?? "nil".Eval();
    }
}
