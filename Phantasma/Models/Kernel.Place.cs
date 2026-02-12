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
        // Handle variadic array wrapper from IronScheme.
        if (place is object[] arr && arr.Length > 0)
            place = arr[0];
        
        var p = place as Place;
        return p?.Width ?? 0;
    }
    
    public static object PlaceGetHeight(object place)
    {
        // Handle variadic array wrapper from IronScheme.
        if (place is object[] arr && arr.Length > 0)
            place = arr[0];
        
        var p = place as Place;
        return p?.Height ?? 0;
    }
    
    public static object PlaceSetTerrain(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-place-set-terrain] Expected 2 args (loc terrain), got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        if (!UnpackLocation(args[0], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-place-set-terrain] Invalid location list");
            return "nil".Eval();
        }
        
        var t = args[1] as Terrain;
        
        if (place != null && t != null)
        {
            if (x >= 0 && x < place.Width && y >= 0 && y < place.Height)
            {
                place.TerrainGrid[x, y] = t;
            }
        }
        
        return "nil".Eval();
    }
    
    public static object PlaceGetTerrain(object[] args)
    {
        if (args == null || args.Length < 1)
        {
            Console.WriteLine($"[kern-place-get-terrain] Expected 1 arg (loc), got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        if (!UnpackLocation(args[0], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-place-get-terrain] Invalid location list");
            return "nil".Eval();
        }
        
        if (x >= 0 && x < place.Width && y >= 0 && y < place.Height)
        {
            var terrain = place.TerrainGrid[x, y];
            return terrain ?? "nil".Eval();
        }
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-place-set-current place)
    /// Sets the place as the current game place.
    /// </summary>
    public static object PlaceSetCurrent(object place)
    {
        // Handle variadic array wrapper from IronScheme.
        if (place is object[] arr && arr.Length > 0)
            place = arr[0];
        
        if (place is Place p)
        {
            Phantasma.RegisterObject(KEY_CURRENT_PLACE, p);
            Console.WriteLine($"Registered current place: {p.Name}");
            return p;
        }
    
        Console.WriteLine("[WARNING] kern-place-set-current: Invalid place object");
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-place-get-name place)
    /// Returns the display name of a place.
    /// </summary>
    public static object PlaceGetName(object placeObj)
    {
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length > 0)
            placeObj = arr[0];
        
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
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length > 0)
            placeObj = arr[0];
        
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
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length >= 2)
        {
            dirObj = arr[1];
            placeObj = arr[0];
        }
        
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
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length > 0)
            placeObj = arr[0];
        
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
    public static object PlaceIsWrapping(object[] args)
    {
        if (args == null || args.Length < 1) return "#f".Eval();
        
        object placeObj = args[0];
        
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
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length > 0)
            placeObj = arr[0];
        
        if (placeObj is not Place place) return Cons.FromList(new List<object>());
        var beings = new List<object>();
        foreach (var obj in place.Objects)
            if (obj is Being being) beings.Add(being);
        return Cons.FromList(beings);
    }

    /// <summary>
    /// (kern-place-is-passable place x y obj)
    /// </summary>
    public static object PlaceIsPassable(object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Console.WriteLine($"[kern-place-is-passable] Expected 2 args, got {args?.Length ?? 0}");
            return false;
        }
        
        if (!UnpackLocation(args[0], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-place-is-passable] Invalid location list");
            return false;
        }
        
        var obj = args[1] as Object;
        return place.IsPassable(x, y, obj);
    }

    /// <summary>
    /// (kern-place-is-hazardous place x y)
    /// </summary>
    public static object PlaceIsHazardous(object[] args)
    {
        if (args == null || args.Length < 1)
        {
            Console.WriteLine($"[kern-place-is-hazardous] Expected 2 args (loc obj), got {args?.Length ?? 0}");
            return false;
        }
        
        if (!UnpackLocation(args[0], out var place, out int x, out int y))
        {
            Console.WriteLine("[kern-place-is-hazardous] Invalid location list");
            return false;
        }
        
        var terrain = place.GetTerrain(x, y);
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
        // Handle variadic array wrapper from IronScheme.
        if (terrain is object[] arr && arr.Length >= 2)
        {
            map = arr[1];
            terrain = arr[0];
        }
        
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
        
        return terrain;
    }
    
    /// <summary>
    /// (kern-place-map place)
    /// Get the terrain map from a place.
    /// </summary>
    /// <param name="placeArg"></param>
    /// <returns></returns>
    public static object PlaceMap(object placeArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (placeArg is object[] arr && arr.Length > 0)
            placeArg = arr[0];
        
        var place = ResolveObject<Place>(placeArg);
        
        if (place == null || place.TerrainGrid == null)
        {
            RuntimeError("kern-place-map: null place or map");
            return "nil".Eval();
        }

        TerrainMap map = new TerrainMap(place.Tag.Replace("p_", "m_"), place.Width, place.Height);
        map.TerrainGrid = place.TerrainGrid;
        
        return map;
    }
    
    /// <summary>
    /// (kern-place-synch place)
    /// Synchronize a place.
    /// </summary>
    /// <param name="placeArg"></param>
    /// <returns></returns>
    public static object PlaceSynch(object placeArg)
    {
        // Handle variadic array wrapper from IronScheme.
        if (placeArg is object[] arr && arr.Length > 0)
            placeArg = arr[0];
        
        var place = ResolveObject<Place>(placeArg);
        
        if (place == null)
        {
            RuntimeError("kern-place-synch: null place");
            return "nil".Eval();
        }
        
        place.Synchronize();
        
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-place-get-objects place)
    /// et all objects in a place.
    /// </summary>
    /// <param name="placeObj"></param>
    /// <returns></returns>
    public static object PlaceGetObjects(object placeObj)
    {
        // Handle variadic array wrapper from IronScheme.
        if (placeObj is object[] arr && arr.Length > 0)
            placeObj = arr[0];
        
        if (placeObj is not Place place) 
            return Cons.FromList(new List<object>());
    
        return Cons.FromList(new List<object>(place.Objects));
    }
    
    /// <summary>
    /// (kern-place-add-subplace parent-place subplace x y)
    /// Adds a subplace (town, dungeon, etc.) to a parent place at the given coordinates.
    /// This sets up the parent-child relationship so characters can enter/exit.
    /// </summary>
    public static object PlaceAddSubplace(object[] args)
    {
        if (args == null || args.Length < 4)
        {
            Console.WriteLine($"[kern-place-add-subplace] Expected 4 args, got {args?.Length ?? 0}");
            return "nil".Eval();
        }
        
        object parentObj = args[0];
        object subplaceObj = args[1];
        int x = ToInt(args[2], 0);
        int y = ToInt(args[3], 0);
        
        if (parentObj is not Place parent)
        {
            Console.WriteLine("[WARNING] kern-place-add-subplace: invalid parent place");
            return "nil".Eval();
        }
        
        if (subplaceObj is not Place subplace)
        {
            Console.WriteLine("[WARNING] kern-place-add-subplace: invalid subplace");
            return "nil".Eval();
        }
        
        // Set up the subplace's location (its position on the parent map).
        subplace.Location = new Location(parent, x, y);
        
        // Register the subplace with the parent.
        parent.AddSubplace(subplace, x, y);
        
        Console.WriteLine($"[kern-place-add-subplace] Added '{subplace.Name}' to '{parent.Name}' at ({x}, {y})");
        
        return subplace;
    }
}
