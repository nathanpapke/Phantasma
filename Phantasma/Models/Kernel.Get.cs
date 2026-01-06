using System;
using System.Collections.Generic;
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
    public static object InLineOfSight(object p1, object x1, object y1, object p2, object x2, object y2)
    {
        if (p1 is not Place place1 || p2 is not Place place2) return false;
        if (place1 != place2) return false;
        return place1.IsInLineOfSight(Convert.ToInt32(x1), Convert.ToInt32(y1), 
            Convert.ToInt32(x2), Convert.ToInt32(y2));
    }

    /// <summary>
    /// (kern-get-distance place1 x1 y1 place2 x2 y2)
    /// </summary>
    public static object GetDistance(object p1, object x1, object y1, object p2, object x2, object y2)
    {
        if (p1 is not Place place1 || p2 is not Place place2) return -1;
        if (place1 != place2) return -1;
        return place1.GetFlyingDistance(Convert.ToInt32(x1), Convert.ToInt32(y1),
            Convert.ToInt32(x2), Convert.ToInt32(y2));
    }

    /// <summary>
    /// (kern-get-objects-at place x y)
    /// </summary>
    public static object GetObjectsAt(object placeObj, object xObj, object yObj)
    {
        if (placeObj is not Place place) return Cons.FromList(new List<object>());
        var objects = new List<object>(place.GetObjectsAt(Convert.ToInt32(xObj), Convert.ToInt32(yObj)));
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
            objType = Phantasma.GetRegisteredObject(typeTag) as ObjectType;
        }
        
        if (objType == null)
        {
            // Not an ObjectType - could be Character, etc. Return nil.
            return "nil".Eval();
        }
        
        // Return the interaction handler (gifc) or nil.
        return objType.InteractionHandler ?? "nil".Eval();
    }
}
