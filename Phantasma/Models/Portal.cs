namespace Phantasma.Models;

/// <summary>
/// Portals (stairs, exits)
/// </summary>
public class Portal : Object
{
    public override ObjectLayer Layer => ObjectLayer.Portal;
    
    // Portal-specific Properties
    public string? DestinationMap { get; set; }
    public int DestinationX { get; set; }
    public int DestinationY { get; set; }
}
