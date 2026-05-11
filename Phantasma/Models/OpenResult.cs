namespace Phantasma.Models;

/// <summary>
/// Result of attempting to open a container on the ground.
/// </summary>
public enum OpenResult
{
    /// <summary>Container was opened successfully.</summary>
    Okay,
    
    /// <summary>Container is too far away to reach.</summary>
    TooFar,
    
    /// <summary>Object cannot be opened (not a container, or not applicable).</summary>
    NotApplicable,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
