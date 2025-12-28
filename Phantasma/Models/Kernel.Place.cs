using System;
using System.Collections.Generic;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
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
    
    /// <summary>
    /// (kern-place-get-name place)
    /// Returns the display name of a place.
    /// </summary>
    public static object PlaceGetName(object placeObj)
    {
        if (placeObj is Place place)
        {
            return place.Name ?? "";
        }
        
        Console.WriteLine("[WARNING] kern-place-get-name: null or invalid place");
        return "";
    }
    
    /// <summary>
    /// (kern-place-get-location place)
    /// Returns the parent location of a place as (parent-place x y), or nil if none.
    /// </summary>
    public static object PlaceGetLocation(object placeObj)
    {
        if (placeObj is not Place place)
        {
            Console.WriteLine("[WARNING] kern-place-get-location: null or invalid place");
            return "#f".Eval();
        }
        
        // If place has no parent, return nil/false.
        if (place.Location.Place == null)
        {
            return "#f".Eval();
        }
        
        // Return as list: (parent-place x y)
        return Builtins.List(
            place.Location.Place,
            place.Location.X,
            place.Location.Y
        );
    }
    
    /// <summary>
    /// (kern-place-get-neighbor place direction)
    /// Returns the neighboring place in the given direction (UP or DOWN only).
    /// </summary>
    public static object PlaceGetNeighbor(object placeObj, object dirObj)
    {
        if (placeObj is not Place place)
        {
            Console.WriteLine("[WARNING] kern-place-get-neighbor: null or invalid place");
            return "#f".Eval();
        }
        
        int dir = Convert.ToInt32(dirObj);
        
        Place? neighbor = dir switch
        {
            Common.UP => place.Above,
            Common.DOWN => place.Below,
            _ => null  // Only UP/DOWN supported
        };
        
        if (neighbor != null)
            return neighbor;
        
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-place-is-wilderness place)
    /// Returns #t if place is wilderness, #f otherwise.
    /// </summary>
    public static object PlaceIsWilderness(object placeObj)
    {
        if (placeObj is Place place)
        {
            return place.Wilderness ? "#t".Eval() : "#f".Eval();
        }
        
        Console.WriteLine("[WARNING] kern-place-is-wilderness: null or invalid place");
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-place-is-wrapping place)
    /// Returns #t if place wraps at edges, #f otherwise.
    /// </summary>
    public static object PlaceIsWrapping(object placeObj)
    {
        if (placeObj is Place place)
        {
            return place.Wraps ? "#t".Eval() : "#f".Eval();
        }
        
        Console.WriteLine("[WARNING] kern-place-is-wrapping: null or invalid place");
        return "#f".Eval();
    }

    /// <summary>
    /// (kern-place-get-beings place)
    /// </summary>
    public static object PlaceGetBeings(object placeObj)
    {
        if (placeObj is not Place place) return Cons.FromList(new List<object>());
        var beings = new List<object>();
        foreach (var obj in place.Objects)
            if (obj is Being being) beings.Add(being);
        return Cons.FromList(beings);
    }

    /// <summary>
    /// (kern-place-is-passable place x y obj)
    /// </summary>
    public static object PlaceIsPassable(object placeObj, object xObj, object yObj, object objArg)
    {
        if (placeObj is not Place place) return false;
        return place.IsPassable(Convert.ToInt32(xObj), Convert.ToInt32(yObj), objArg as Object);
    }

    /// <summary>
    /// (kern-place-is-hazardous place x y)
    /// </summary>
    public static object PlaceIsHazardous(object placeObj, object xObj, object yObj)
    {
        if (placeObj is not Place place) return false;
        var terrain = place.GetTerrain(Convert.ToInt32(xObj), Convert.ToInt32(yObj));
        return terrain?.IsHazardous ?? false;
    }
    /// <summary>
    /// (kern-terrain-set-combat-map terrain map)
    /// Sets the combat map used when combat occurs on this terrain type.
    /// 
    /// Example:
    /// (kern-terrain-set-combat-map t_grass m_grass_combat)
    /// </summary>
    /// <param name="terrain">Terrain object to modify</param>
    /// <param name="map">TerrainMap to use for combat on this terrain</param>
    /// <returns>The terrain object (for chaining).</returns>
    public static object TerrainSetCombatMap(object terrain, object map)
    {
        // Resolve terrain.
        Terrain? t = null;
        if (terrain is Terrain ter)
            t = ter;
        else if (terrain is string terTag)
            t = Phantasma.GetRegisteredObject(terTag.TrimStart('\'').Trim('"')) as Terrain;
        
        if (t == null)
        {
            Console.WriteLine("[ERROR] kern-terrain-set-combat-map: null or invalid terrain");
            return "#f".Eval();
        }
        
        // Resolve terrain map.
        TerrainMap? combatMap = null;
        if (map is TerrainMap tm)
            combatMap = tm;
        else if (map is string mapTag)
        {
            var mapObj = Phantasma.GetRegisteredObject(mapTag.TrimStart('\'').Trim('"'));
            if (mapObj is TerrainMap resolvedMap)
                combatMap = resolvedMap;
        }
        
        // Set the combat map on the terrain.
        t.CombatMap = combatMap;
        
        Console.WriteLine($"  Set combat map for terrain '{t.Name}'");
        
        return terrain;
    }
}
