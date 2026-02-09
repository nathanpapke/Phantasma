using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-being-pathfind-to being place x y)
    /// Pathfind toward destination and take one step.
    /// Returns #t if moved, #f otherwise.
    /// </summary>
    public static object BeingPathfindTo(object[] args)
    {
        // Unpack arguments - handle both direct calls and apply
        object beingObj, placeObj, xObj, yObj;
        
        if (args.Length >= 4)
        {
            beingObj = args[0];
            placeObj = args[1];
            xObj = args[2];
            yObj = args[3];
        }
        else if (args.Length == 1 && args[0] is Cons list)
        {
            // Called via apply with a single list argument
            var items = list.ToList();
            if (items.Count < 4)
            {
                Console.WriteLine($"[kern-being-pathfind-to] Expected 4 args in list, got {items.Count}");
                return "#f".Eval();
            }
            beingObj = items[0];
            placeObj = items[1];
            xObj = items[2];
            yObj = items[3];
        }
        else
        {
            Console.WriteLine($"[kern-being-pathfind-to] Expected 4 args, got {args.Length}");
            return "#f".Eval();
        }
        
        // Handle array wrapper from IronScheme.
        if (beingObj is object[] arr && arr.Length > 0)
            beingObj = arr[0];
        
        if (beingObj is not Being being)
        {
            Console.WriteLine($"[kern-being-pathfind-to] Invalid being: {beingObj?.GetType().Name}");
            return "#f".Eval();
        }
        
        if (placeObj is not Place place)
        {
            // Try to resolve by tag.
            place = Phantasma.GetRegisteredObject(placeObj?.ToString() ?? "") as Place;
            if (place == null)
            {
                Console.WriteLine($"[kern-being-pathfind-to] Invalid place: {placeObj?.GetType().Name}");
                return "#f".Eval();
            }
        }
        
        int x = Convert.ToInt32(xObj);
        int y = Convert.ToInt32(yObj);
        
        return being.PathFindTo(place, x, y);
    }
    
    /// <summary>
    /// (kern-being-is-hostile? being1 being2)
    /// Check if two beings are hostile to each other.
    /// Returns #t if hostile, #f otherwise.
    /// </summary>
    public static object BeingIsHostile(object being1Obj, object being2Obj)
    {
        if (being1Obj is object[] arr && arr.Length >= 2)
        {
            being1Obj = arr[0];
            being2Obj = arr[1];
        }
        
        if (being1Obj is not Being being1 || being2Obj is not Being being2)
        {
            Console.WriteLine("[kern-being-is-hostile?] Invalid being(s)");
            return "#f".Eval();
        }

        var dtable = Phantasma.MainSession?.DiplomacyTable;
        int f1 = being1.GetCurrentFaction();
        int f2 = being2.GetCurrentFaction();
        
        if (dtable != null)
            return dtable.AreHostile(f1, f2);
        else
            return (f1 != f2);
    }
    
    /// <summary>
    /// (kern-being-get-visible-hostiles being)
    /// Get list of visible hostile beings.
    /// Returns Scheme list of beings.
    /// </summary>
    public static object BeingGetVisibleHostiles(object beingObj)
    {
        // Unwrap varargs array from IronScheme.
        if (beingObj is object[] args)
            beingObj = args[0];

        var being = beingObj as Being;
        if (being == null || being.Position?.Place == null)
        {
            RuntimeError("kern-being-get-visible-hostiles: null being or place");
            return "'()".Eval();
        }
        
        var place = being.Position.Place;
        var dtable = Phantasma.MainSession?.DiplomacyTable;
        // NOTE: dtable CAN be null — we handle it in the loop with fallback.
        
        int myFaction = being.GetCurrentFaction();
        int visionRadius = being is Character ch ? ch.GetVisionRadius() : 10;
        int myX = being.GetX();
        int myY = being.GetY();
        
        var hostiles = new List<object>();
        
        foreach (var obj in place.Objects)
        {
            if (obj is not Being other || other == being)
                continue;
            
            // Check hostility.
            int otherFaction = other.GetCurrentFaction();
            bool isHostile;
            if (dtable != null)
            {
                isHostile = dtable.AreHostile(myFaction, otherFaction);
            }
            else
            {
                // Fallback to no diplomacy table: different faction = hostile.
                // This matches the behavior default AI already uses.
                isHostile = (myFaction != otherFaction);
            }
            
            if (!isHostile)
                continue;
            
            // Check visibility.
            if (!other.IsVisible())
                continue;

            // Check distance.
            int dist = place.GetFlyingDistance(myX, myY, other.GetX(), other.GetY());
            if (dist > visionRadius)
                continue;

            // Check line of sight.
            if (!place.IsInLineOfSight(myX, myY, other.GetX(), other.GetY()))
                continue;
            
            hostiles.Add(other);
        }
        
        if (hostiles.Count == 0)
            return "'()".Eval();
        
        // Build Cons list (right-fold).
        object result = "'()".Eval();
        for (int i = hostiles.Count - 1; i >= 0; i--)
        {
            result = new Cons(hostiles[i], result);
        }
        
        Console.WriteLine(result.ToString());
        return result;
    }
    
    /// <summary>
    /// (kern-being-set-base-faction being faction)
    /// Set the being's base faction.
    /// Returns the being.
    /// </summary>
    public static object BeingSetBaseFaction(object beingObj, object factionObj)
    {
        if (beingObj is object[] arr && arr.Length >= 2)
        {
            beingObj = arr[0];
            factionObj = arr[1];
        }
        
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-set-base-faction] Invalid being");
            return "#f".Eval();
        }

        int faction = Convert.ToInt32(factionObj);
        being.SetBaseFaction(faction);

        return being;
    }
    
    /// <summary>
    /// (kern-being-get-base-faction being)
    /// Get the being's base faction.
    /// Returns faction ID as integer.
    /// </summary>
    public static object BeingGetBaseFaction(object beingObj)
    {
        // Unwrap varargs array from IronScheme.
        if (beingObj is object[] args)
            beingObj = args[0];
        
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-get-base-faction] Invalid being");
            return -1;  // INVALID_FACTION
        }

        return being.GetBaseFaction();
    }
    
    /// <summary>
    /// (kern-being-get-current-faction being)
    /// Get the being's current faction (may differ from base if charmed, etc.).
    /// Returns faction ID as integer.
    /// </summary>
    public static object BeingGetCurrentFaction(object beingObj)
    {
        // Unwrap varargs array from IronScheme.
        if (beingObj is object[] args)
            beingObj = args[0];
        
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-get-current-faction] Invalid being");
            return -1;  // INVALID_FACTION
        }

        return being.GetCurrentFaction();
    }
}
