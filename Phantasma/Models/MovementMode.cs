namespace Phantasma.Models;

/// <summary>
/// Movement Mode - determines how a character/object moves through terrain.
/// The Index maps to the passability table to determine movement costs.
/// </summary>
public struct MovementMode
{
    /// <summary>Tag for script reference (e.g., "mmode-walk")</summary>
    public string Tag;
    
    /// <summary>Display name for UI (e.g., "Walking")</summary>
    public string Name;
    
    /// <summary>Index into the passability table</summary>
    public int Index;
    
    /// <summary>
    /// Standard movement mode indices.
    /// These match the rows in the passability table.
    /// </summary>
    public enum ModeIndex
    {
        Walking = 0,
        Swimming = 1,
        Flying = 2,
        Sailing = 3,
        Phasing = 4,    // Can pass through walls
        Climbing = 5    // Can scale mountains
    }
    
    /// <summary>
    /// Create a movement mode with the given parameters.
    /// </summary>
    public MovementMode(string tag, string name, int index)
    {
        Tag = tag;
        Name = name;
        Index = index;
    }
    
    /// <summary>
    /// Create a movement mode from a standard index.
    /// </summary>
    public MovementMode(string tag, string name, ModeIndex index)
        : this(tag, name, (int)index)
    {
    }
    
    public override string ToString() => $"{Name} ({Tag})";
}
