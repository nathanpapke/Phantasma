namespace Phantasma.Models;

/// <summary>
/// Missiles (arrows, spells in flight)
/// </summary>
public class Missile : Object
{
    public override ObjectLayer Layer => ObjectLayer.Missile;
    
    // Object Type this Missile R   epresents (arrow, bolt, etc)
    private ArmsType objectType;
    
    // Animation Flags
    private int flags;
    
    // Hit Detection
    private bool hit;
    private Object? struck;
    
    // Projectile State
    public Being? Source { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public Being? StruckTarget { get; set; }
    
    /// <summary>
    /// Get the sprite for this missile (from its object type).
    /// </summary>
    public Sprite? Sprite
    {
        get =>  objectType?.Sprite;
        set
        {
            if (objectType != null)
                objectType.Sprite = value;
        }
    }

    public enum MissileFlags
    {
        None = 0,
        IgnoreLineOfSight = 1 << 0,
        HitParty = 1 << 1
    }
    
    /// <summary>
    /// Create a missile from an arms type (the projectile).
    /// </summary>
    /// <param name="missileType">The type of projectile (arrow, bolt, etc)</param>
    public Missile(ArmsType missileType)
    {
        this.objectType = missileType;
        this.Name = missileType.Name;
        this.Sprite = missileType.Sprite;
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
    public Object? GetStruck()
    {
        return struck;
    }
    
    /// <summary>
    /// Check if missile hit something.
    /// </summary>
    public bool HitTarget()
    {
        return hit;
    }
    
    /// <summary>
    /// Called by Screen.AnimateProjectile() as missile enters each tile.
    /// Returns false to stop the missile (hit something).
    /// </summary>
    /// <param name="place">The place/map</param>
    /// <param name="x">Tile X coordinate</param>
    /// <param name="y">Tile Y coordinate</param>
    /// <returns>True to continue flying, false to stop</returns>
    public bool EnterTile(Place place, int x, int y)
    {
        // Check line of sight (unless IGNORE_LOS flag is set).
        // This prevents missiles from going through walls/trees.
        if ((flags & (int)MissileFlags.IgnoreLineOfSight) == 0)
        {
            if (place.GetVisibility(x, y) == 0)
            {
                // Hit an obstacle.
                return false;
            }
        }
        
        // Check for party members (if HIT_PARTY flag is set).
        // This allows cannons to damage the player party.
        if ((flags & (int)MissileFlags.HitParty) != 0)
        {
            // Check for any party member at this location.
            var person = place.GetBeingAt(x, y); //.GetPartyAt(x, y);
            foreach (Character member in Phantasma.MainSession.Party.Members)
            {
                if (person == member)
                {
                    struck = person;
                    hit = true;
                    return false; // Stop - we hit the party
                }
            }

            // Also check if player is at this exact location.
            var playerParty = Phantasma.MainSession.Party;
            if (playerParty != null && 
                playerParty.GetPlace() == place &&
                playerParty.GetX() == x && 
                playerParty.GetY() == y)
            {
                struck = playerParty;
                hit = true;
                return false; // Stop - we hit the player
            }
        }
        
        // Continue flying.
        return true;
    }
}
