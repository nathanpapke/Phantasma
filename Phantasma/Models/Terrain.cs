namespace Phantasma.Models;

public class Terrain
{
    public string Name { get; set; }
    public string Color { get; set; }      // Hex color code
    public int PassabilityClass { get; set; }   // Passability class for lookup in passability table
    public bool IsPassable
    { 
        get
        {
            // Based on Nazghul passability classes:
            // 0-5: Passable (none, grass, road, forest, hills, shallow)
            // 6+: Impassable (water, deep, mountain, wall, lava, etc.)
            return PassabilityClass < 6;
        }
        set { /* legacy setter, ignore */ }
    }
    public float MovementCost { get; set; } // (1.0 = normal speed, 2.0 = half speed, etc.)
    public bool IsHazardous { get; set; }
    public string Effect { get; set; }     // Special effect (poison, etc.)
    public int Light { get; set; }  // Light emitted by this terrain (for future use)
    public byte Alpha { get; set; } // Alpha transparency (for overlays, fog, etc.)
    public bool Transparent
    {
        get
        {
            // Mountains and walls block vision, other terrain is transparent.
            return PassabilityClass != 8 && PassabilityClass != 9;
        }
        set { /* legacy setter, ignore */ }
    }
    public char DisplayChar { get; set; }  // ASCII character for display
        
    // For sprite-based rendering.
    public Sprite Sprite { get; set; }
        
    public Terrain()
    {
        Color = "#808080";
        IsPassable = true;
        Transparent = true;
    }
        
    public Terrain(string name, bool isPassable = true, float movementCost = 1.0f)
    {
        Name = name;
        Color = "#808080";
        IsPassable = isPassable;
        MovementCost = movementCost;
        IsHazardous = false;
        PassabilityClass = PassabilityTable.PCLASS_NONE;
        Light = 0;
        Alpha = 255; // Fully opaque
    }
    
    // Constructor for Scheme/Kernel (kern-mk-terrain)
    public Terrain(string tag, string name, Sprite sprite, int pclass, int alpha, int light)
    {
        Name = name;
        Color = "#808080";
        Sprite = sprite;
        PassabilityClass = pclass;
        Alpha = (byte)alpha;
        Light = light;
        IsPassable = (pclass == 0);  // pclass 0 = passable, 255 = impassable
        Transparent = (alpha < 255);  // Full alpha = blocks vision
        MovementCost = 1.0f;
        IsHazardous = false;
    }
        
    public override string ToString()
    {
        string passStr = IsPassable ? "passable" : "impassable";
        string hazStr = IsHazardous ? ", hazardous" : "";
        return $"Terrain({Name}: {passStr}, cost={MovementCost:F1}{hazStr})";
    }
}
