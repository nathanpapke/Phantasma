namespace Phantasma.Models;

/// <summary>
/// Result of attempting to ready (equip) an armament.
/// </summary>
public enum ReadyResult
{
    /// <summary>Armament was successfully equipped.</summary>
    Readied,
    
    /// <summary>All slots of the correct type are already occupied.</summary>
    NoAvailableSlot,
    
    /// <summary>This species has no slot compatible with this armament type.</summary>
    WrongType,
    
    /// <summary>Armament exceeds the actor's carrying capacity.</summary>
    TooHeavy,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}