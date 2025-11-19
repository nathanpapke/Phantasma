namespace Phantasma.Models;

/// <summary>
/// Mechanisms (levers, buttons, switches)
/// </summary>
public class Mechanism : Object, IBlockingObject
{
    public override ObjectLayer Layer => ObjectLayer.Mechanism;
    
    // Mechanism-specific Properties
    public bool IsActivated { get; set; }
    public bool IsPassable { get; set; } = true;
}
