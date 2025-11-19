namespace Phantasma.Models;

/// <summary>
/// Missiles (arrows, spells in flight)
/// </summary>
public class Missile : Object
{
    public override ObjectLayer Layer => ObjectLayer.Missile;
    
    // Projectile-specific Properties
    public Being? Source { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public int Damage { get; set; }
}
