namespace Phantasma.Models;

/// <summary>
/// Fields (fire, poison, energy)
/// </summary>
public class Field : Object
{
    public override ObjectLayer Layer => ObjectLayer.Field;
    
    // Field-specific Properties
    public bool IsDamaging { get; set; }
    public int DamagePerTurn { get; set; }
    public int Duration { get; set; } = -1; // -1 = permanent
}
