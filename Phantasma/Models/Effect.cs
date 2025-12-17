using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// An effect type defines status effects like poison, protection, haste, etc.
/// This is the TYPE definition - instances are tracked via HookEntry structs on Objects.
/// </summary>
public class Effect
{
    /// <summary>Identifier tag name (e.g., "ef-poison").</summary>
    public string Tag { get; set; } = "";
    
    /// <summary>Short display name (e.g., "Poison").</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Longer description of the effect.</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Scheme closure to execute on the hook trigger.</summary>
    public object? ExecClosure { get; set; }
    
    /// <summary>Scheme closure to execute when effect is first attached.</summary>
    public object? ApplyClosure { get; set; }
    
    /// <summary>Scheme closure to execute when effect is removed.</summary>
    public object? RemoveClosure { get; set; }
    
    /// <summary>Scheme closure to restart effect after save/load.</summary>
    public object? RestartClosure { get; set; }
    
    /// <summary>Single character status code for Ztats window ('P', 'S', 'C', etc.).</summary>
    public char StatusCode { get; set; } = ' ';
    
    /// <summary>Sprite for visual display.</summary>
    public Sprite? Sprite { get; set; }
    
    /// <summary>Detection difficulty class (0 = always visible).</summary>
    public int DetectDC { get; set; } = 0;
    
    /// <summary>If true, multiple instances can stack on one target.</summary>
    public bool Cumulative { get; set; } = false;
    
    /// <summary>Default duration in turns (-1 = permanent).</summary>
    public int Duration { get; set; } = -1;
    
    /// <summary>Which hook this effect attaches to.</summary>
    public HookId HookId { get; set; } = HookId.StartOfTurn;
    
    public override string ToString() => $"Effect({Tag}: {Name})";
}
