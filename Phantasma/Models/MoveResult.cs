namespace Phantasma.Models;

/// <summary>
/// Result of a movement attempt.
/// Used by both engine internals and the agent interface.
/// </summary>
public enum MoveResult
{
    // Engine Results
    
    /// <summary>Move succeeded normally.</summary>
    Okay,
    
    /// <summary>Movement would leave the map boundary with no parent place to exit to.</summary>
    OffMap,
    
    /// <summary>Moved into a subplace entrance (town, dungeon, etc.).</summary>
    EnterSubplace,
    
    /// <summary>Destination tile is blocked by impassable terrain.</summary>
    Impassable,
    
    /// <summary>Destination tile is occupied by another being.</summary>
    Occupied,
    
    /// <summary>Movement triggered a combat encounter.</summary>
    EnterCombat,
    
    /// <summary>No current place set on the session.</summary>
    NullPlace,
    
    /// <summary>No valid destination could be determined (e.g. diagonal into subplace).</summary>
    NoDestination,
    
    // Agent Only Results
    
    /// <summary>Exited the current map to a parent place.</summary>
    Exit,
    
    /// <summary>Destination is beyond the actor's movement range this action.</summary>
    TooFar,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver,
    
    /// <summary>Unexpected or unhandled movement result.</summary>
    Unknown
}
