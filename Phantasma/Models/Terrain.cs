namespace Phantasma.Models;

public class Terrain
{
    public string Name { get; set; }
    public char DisplayChar { get; set; }  // ASCII character for display
    public string Color { get; set; }      // Hex color code
    public int PassabilityClass { get; set; }   // Passability class for lookup in passability table
    public bool IsPassable { get; set; }     // Can beings walk through?
    public float MovementCost { get; set; } // (1.0 = normal speed, 2.0 = half speed, etc.)
    public bool IsHazardous { get; set; }
    public string Effect { get; set; }     // Special effect (poison, etc.)
    public int Light { get; set; }  // Light emitted by this terrain (for future use)
    public byte Alpha { get; set; } // Alpha transparency (for overlays, fog, etc.)
    public bool Transparent { get; set; } = true;  // Can see through?
        
    // For sprite-based rendering.
    public Sprite Sprite { get; set; }
        
    public Terrain()
    {
        IsPassable = true;
        Transparent = true;
    }
        
    public Terrain(string name, bool isPassable = true, float movementCost = 1.0f)
    {
        Name = name;
        IsPassable = isPassable;
        MovementCost = movementCost;
        IsHazardous = false;
        PassabilityClass = PassabilityTable.PCLASS_NONE;
        Light = 0;
        Alpha = 255; // Fully opaque
    }
        
    public override string ToString()
    {
        string passStr = IsPassable ? "passable" : "impassable";
        string hazStr = IsHazardous ? ", hazardous" : "";
        return $"Terrain({Name}: {passStr}, cost={MovementCost:F1}{hazStr})";
    }
    
    
    /// <summary>
    /// Common terrain types for quick access
    /// </summary>
    /*
    // Basic passable terrains
    public static Terrain Grass => new Terrain("grass", true, 1.0f) 
    { 
        PassabilityClass = 1 
    };
    
    public static Terrain Dirt => new Terrain("dirt", true, 1.0f) 
    { 
        PassabilityClass = 1 
    };
    
    public static Terrain Road => new Terrain("road", true, 0.75f) 
    { 
        PassabilityClass = 2 // Faster movement
    };
    
    public static Terrain Cobblestone => new Terrain("cobblestone", true, 0.8f) 
    { 
        PassabilityClass = 2 
    };
    
    // Slow passable terrains
    public static Terrain Forest => new Terrain("forest", true, 1.5f) 
    { 
        PassabilityClass = 3 // Slows movement
    };
    
    public static Terrain Hills => new Terrain("hills", true, 1.3f) 
    { 
        PassabilityClass = 4 
    };
    
    public static Terrain Shallow => new Terrain("shallow", true, 2.0f) 
    { 
        PassabilityClass = 5 // Can wade slowly
    };
    
    // Impassable terrains
    public static Terrain Water => new Terrain("water", false, 1.0f) 
    { 
        PassabilityClass = 6 // Need boat
    };
    
    public static Terrain DeepWater => new Terrain("deep_water", false, 1.0f) 
    { 
        PassabilityClass = 7 
    };
    
    public static Terrain Mountain => new Terrain("mountain", false, 1.0f) 
    { 
        PassabilityClass = 8 // Can't climb
    };
    
    public static Terrain Wall => new Terrain("wall", false, 1.0f) 
    { 
        PassabilityClass = 9 
    };
    
    // Hazardous terrains
    public static Terrain Lava => new Terrain("lava", false, 1.0f) 
    { 
        PassabilityClass = 10,
        IsHazardous = true,
        Light = 5 // Emits light
    };
    
    public static Terrain Swamp => new Terrain("swamp", true, 2.5f) 
    { 
        PassabilityClass = 11,
        IsHazardous = true // Poison damage
    };
    
    public static Terrain Fire => new Terrain("fire", true, 1.5f) 
    { 
        PassabilityClass = 12,
        IsHazardous = true,
        Light = 3
    };
    
    public static Terrain Ice => new Terrain("ice", true, 0.9f) 
    { 
        PassabilityClass = 13 // Slippery, might fall
    };
     */
}
