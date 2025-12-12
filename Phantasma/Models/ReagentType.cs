namespace Phantasma.Models;

/// <summary>
/// ReagentType defines a magical reagent (mandrake, nightshade, etc).
/// Reagents are consumed when casting certain spells.
/// </summary>
public class ReagentType : ObjectType
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Single character identifier (e.g., 'a' for ash, 'm' for mandrake).
    /// Used in Ultima-style reagent UI: "ABGMNPS"
    /// </summary>
    public char DisplayChar { get; set; }
    
    /// <summary>
    /// Sprite for displaying the reagent (optional).
    /// </summary>
    public Sprite? Sprite { get; set; }
    
    /// <summary>
    /// Description of the reagent.
    /// </summary>
    public string Description { get; set; } = "";
    
    public override string ToString()
    {
        return Name;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ReagentType other)
        {
            return Tag == other.Tag;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return Tag.GetHashCode();
    }
}