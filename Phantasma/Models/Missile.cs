namespace Phantasma.Models;

/// <summary>
/// Missiles (arrows, spells in flight)
/// </summary>
public class Missile : Object
{
    public override ObjectLayer Layer => ObjectLayer.Missile;
    
    // The object type this missile represents (arrow, bolt, etc)
    private ArmsType objectType;
    
    // Projectile State
    public Being? Source { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public Being? StruckTarget { get; set; }
    
    /// <summary>
    /// Create a missile from an arms type (the projectile).
    /// </summary>
    /// <param name="missileType">The type of projectile (arrow, bolt, etc)</param>
    public Missile(ArmsType missileType)
    {
        this.objectType = missileType;
        this.Name = missileType.Name;
    }
    
    /// <summary>
    /// Get the object type of this missile.
    /// </summary>
    public ArmsType GetObjectType()
    {
        return objectType;
    }
    
    /// <summary>
    /// Get what this missile struck (if anything).
    /// </summary>
    public Being? GetStruck()
    {
        return StruckTarget;
    }
    
    /// <summary>
    /// Check if missile hit something.
    /// </summary>
    public bool HitTarget()
    {
        return StruckTarget != null;
    }
    
    /// <summary>
    /// Animate missile flight from origin to target.
    /// For Task 15, this is simplified - just checks if we hit.
    /// Full animation implementation will come in Task 18 (Missiles and Projectiles).
    /// </summary>
    /// <param name="originX">Starting X</param>
    /// <param name="originY">Starting Y</param>
    /// <param name="targetX">Ending X</param>
    /// <param name="targetY">Ending Y</param>
    /// <param name="flags">Animation flags (ignored for now)</param>
    /// <returns>True if animation completed</returns>
    public bool Animate(int originX, int originY, int targetX, int targetY, int flags)
    {
        // TODO: Implement actual missile flight animation.
        // For now, just check if there's a being at the target.
        
        if (Position?.Place == null)
            return false;
        
        // Check for being at target location.
        var target = Position.Place.GetBeingAt(targetX, targetY);
        if (target != null)
        {
            StruckTarget = target;
            return true;
        }
        
        StruckTarget = null;
        return true; // Animation "completed" even if nothing hit.
    }
}
