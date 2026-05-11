namespace Phantasma.Models;

/// <summary>
/// Result of attempting to unready (unequip) an armament.
/// </summary>
public enum UnreadyResult
{
    /// <summary>Armament was successfully unequipped.</summary>
    Okay,
    
    /// <summary>Armament was not found in any equipment slot.</summary>
    NotFound,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
