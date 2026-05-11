namespace Phantasma.Models;

/// <summary>
/// Result of an attack attempt.
/// </summary>
public enum AttackResult
{
    /// <summary>Attack was executed successfully (hit or miss resolved normally).</summary>
    Okay,
    
    /// <summary>Attack had no effect (e.g. target is immune).</summary>
    NoEffect,
    
    /// <summary>Weapon requires ammo and none remains.</summary>
    NoAmmo,
    
    /// <summary>Target is beyond the weapon's maximum range.</summary>
    TooFar,
    
    /// <summary>Attack roll failed; the blow did not connect.</summary>
    Missed,
    
    /// <summary>Attack was hindered by terrain or obstruction.</summary>
    Hindered,
    
    /// <summary>Attack was blocked by the target's shield or parry.</summary>
    Blocked,
    
    /// <summary>No weapon is currently readied for this attack type.</summary>
    NotReadied,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver,
    
    /// <summary>Target successfully defended against the attack.</summary>
    Defended,
    
    /// <summary>No valid target was specified or found.</summary>
    NoTarget
}
