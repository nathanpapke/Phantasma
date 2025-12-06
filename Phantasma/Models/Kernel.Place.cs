using System;
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
}
