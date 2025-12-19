namespace Phantasma.Models;

/// <summary>
/// Represents a phase of an astral body (e.g., full moon, new moon).
/// </summary>
public class Phase
{
    public Sprite Sprite { get; set; }
    public string Name { get; set; }
    public int MaxLight { get; set; }
    
    public Phase() { }
    
    public Phase(Sprite sprite, int maxLight, string name)
    {
        Sprite = sprite;
        MaxLight = maxLight;
        Name = name;
    }
}
