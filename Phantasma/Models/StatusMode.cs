namespace Phantasma.Models;

/// <summary>
/// Modes for the status window display.
/// </summary>
public enum StatusMode
{
    /// <summary>
    /// Show compact party list with HP/status
    /// </summary>
    ShowParty,
    
    /// <summary>
    /// Select a party member from list
    /// </summary>
    SelectCharacter,
    
    /// <summary>
    /// Show detailed character stats (Z command)
    /// </summary>
    Ztats,
    
    /// <summary>
    /// Ready/equip weapons (R command)
    /// </summary>
    Ready,
    
    /// <summary>
    /// Use items from inventory (U command)
    /// </summary>
    Use,
    
    /// <summary>
    /// Display scrollable text page
    /// </summary>
    Page,
    
    /// <summary>
    /// Trade interface (buy/sell)
    /// </summary>
    Trade,
    
    /// <summary>
    /// Mix reagents for spells
    /// </summary>
    MixReagents,
    
    /// <summary>
    /// Generic selectable list
    /// </summary>
    GenericList,
    
    /// <summary>
    /// String list selection
    /// </summary>
    StringList
}

/// <summary>
/// Scroll directions for status window navigation
/// </summary>
public enum StatusScrollDir
{
    ScrollUp,
    ScrollDown,
    ScrollLeft,
    ScrollRight,
    ScrollPageUp,
    ScrollPageDown
}

/// <summary>
/// Types of selections that can be made in status window
/// </summary>
public enum StatusSelection
{
    Character,
    InventoryItem,
    TradeItem,
    Reagents,
    Generic,
    String
}

/// <summary>
/// Views available in Ztats mode
/// </summary>
public enum ZtatsView
{
    ViewMember = 0,
    ViewArmaments,
    ViewReagents,
    ViewSpells,
    ViewItems,
    ViewMisc,
    NumViews
}
