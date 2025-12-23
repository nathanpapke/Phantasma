using System;
using System.Collections.Generic;
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
    public static object BeingPathfindTo(object beingObj, object placeObj, object xObj, object yObj)
    {
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-pathfind-to] Invalid being");
            return false;
        }

        if (placeObj is not Place place)
        {
            Console.WriteLine("[kern-being-pathfind-to] Invalid place");
            return false;
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
        if (being1Obj is not Being being1 || being2Obj is not Being being2)
        {
            Console.WriteLine("[kern-being-is-hostile?] Invalid being(s)");
            return false;
        }

        var dtable = Phantasma.MainSession?.DiplomacyTable;
        if (dtable == null)
            return false;

        int f1 = being1.GetCurrentFaction();
        int f2 = being2.GetCurrentFaction();

        return dtable.AreHostile(f1, f2);
    }
    
    /// <summary>
    /// (kern-being-get-visible-hostiles being)
    /// Get list of visible hostile beings.
    /// Returns Scheme list of beings.
    /// </summary>
    public static object BeingGetVisibleHostiles(object beingObj)
    {
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-get-visible-hostiles] Invalid being");
            return Cons.FromList(new List<object>());
        }

        var place = being.GetPlace();
        if (place == null)
            return Cons.FromList(new List<object>());

        var dtable = Phantasma.MainSession?.DiplomacyTable;
        if (dtable == null)
            return Cons.FromList(new List<object>());

        var hostiles = new List<object>();
        int myFaction = being.GetCurrentFaction();
        int visionRadius = being.GetVisionRadius();

        foreach (var obj in place.Objects)
        {
            if (obj is not Being other || other == being)
                continue;

            // Check faction hostility.
            int otherFaction = other.GetCurrentFaction();
            if (!dtable.AreHostile(myFaction, otherFaction))
                continue;

            // Check visibility.
            if (!other.IsVisible())
                continue;

            // Check distance (within vision radius).
            int dist = place.GetFlyingDistance(being.GetX(), being.GetY(), other.GetX(), other.GetY());
            if (dist > visionRadius)
                continue;

            // Check line of sight.
            if (!place.IsInLineOfSight(being.GetX(), being.GetY(), other.GetX(), other.GetY()))
                continue;

            hostiles.Add(other);
        }

        return Cons.FromList(hostiles);
    }
    
    /// <summary>
    /// (kern-being-set-base-faction being faction)
    /// Set the being's base faction.
    /// Returns the being.
    /// </summary>
    public static object BeingSetBaseFaction(object beingObj, object factionObj)
    {
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-set-base-faction] Invalid being");
            return false;
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
        if (beingObj is not Being being)
        {
            Console.WriteLine("[kern-being-get-current-faction] Invalid being");
            return -1;  // INVALID_FACTION
        }

        return being.GetCurrentFaction();
    }
}
