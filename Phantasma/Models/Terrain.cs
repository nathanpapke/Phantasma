namespace Phantasma.Models;

public class Terrain
{
    public string Name { get; set; }
    public char DisplayChar { get; set; }  // ASCII character for display
    public string Color { get; set; }      // Hex color code
    public bool Passable { get; set; }     // Can beings walk through?
    public bool Transparent { get; set; } = true;  // Can see through?
    public string Effect { get; set; }     // Special effect (poison, etc.)
        
    // For sprite-based rendering.
    public Sprite Sprite { get; set; }
        
    public Terrain()
    {
        Passable = true;
        Transparent = true;
    }
        
    public bool IsHazardous()
    {
        return !string.IsNullOrEmpty(Effect);
    }
}
