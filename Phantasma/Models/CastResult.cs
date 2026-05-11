namespace Phantasma.Models;

/// <summary>
/// Result of attempting to cast a spell.
/// </summary>
public enum CastResult
{
    /// <summary>Spell was cast successfully.</summary>
    Okay,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
