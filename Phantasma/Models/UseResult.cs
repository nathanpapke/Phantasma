namespace Phantasma.Models;

/// <summary>
/// Result of attempting to use an item.
/// </summary>
public enum UseResult
{
    /// <summary>Item was used successfully.</summary>
    Okay,
    
    /// <summary>Actor has no action points remaining; turn is over.</summary>
    TurnOver
}
