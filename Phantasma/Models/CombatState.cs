namespace Phantasma.Models;

/// <summary>
/// Combat states - can be used in Session to track current combat state.
/// </summary>
public enum CombatState
{
    /// <summary>Not in combat.</summary>
    Done,
    
    /// <summary>Actively fighting.</summary>
    Fighting,
    
    /// <summary>Combat won, looting phase.</summary>
    Looting,
    
    /// <summary>Camping (can be ambushed).</summary>
    Camping
}
