namespace Phantasma.Models;

/// <summary>
/// Hook IDs - when effects trigger.
/// </summary>
public enum HookId
{
    /// <summary>Runs at start of each turn after action points are given.</summary>
    StartOfTurn = 0,
    
    /// <summary>Runs when a new effect is added (can block).</summary>
    AddHook = 1,
    
    /// <summary>Runs when the object takes damage.</summary>
    Damage = 2,
    
    /// <summary>Runs on keystroke (UI).</summary>
    Keystroke = 3,
    
    /// <summary>Number of hook types.</summary>
    NumHooks = 4
}
