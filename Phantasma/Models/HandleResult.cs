namespace Phantasma.Models;

/// <summary>
/// Result of attempting to operate a mechanism (door, lever, portcullis, etc.).
/// </summary>
public enum HandleResult
{
    /// <summary>Mechanism was operated successfully.</summary>
    Okay,
    
    /// <summary>This mechanism cannot be operated (not applicable).</summary>
    NotApplicable,
    
    /// <summary>Mechanism is too far away to reach.</summary>
    TooFar,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
