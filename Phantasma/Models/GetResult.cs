namespace Phantasma.Models;

/// <summary>
/// Result of attempting to pick up an item from the ground.
/// </summary>
public enum GetResult
{
    /// <summary>Item was picked up successfully.</summary>
    Okay,
    
    /// <summary>Item is too far away to reach.</summary>
    TooFar,
    
    /// <summary>Item cannot be picked up (not applicable).</summary>
    NotApplicable,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
