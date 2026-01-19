using System;
using Avalonia.Input;

namespace Phantasma.Models;

/// <summary>
/// Key handler for direction input (arrow keys, numpad).
/// 
/// Used by commands that need a direction: Open, Get, Handle, Search, etc.
/// 
/// When a valid direction key is pressed (or Escape to cancel),
/// invokes the callback with the result and signals completion.
/// </summary>
public class DirectionKeyHandler : IKeyHandler
{
    private readonly Action<Direction?> onComplete;
    
    /// <summary>
    /// Create a direction key handler.
    /// </summary>
    /// <param name="onComplete">
    /// Called when direction is selected or cancelled.
    /// Receives the Direction, or null if cancelled (Escape pressed).
    /// </param>
    public DirectionKeyHandler(Action<Direction?> onComplete)
    {
        this.onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
    }
    
    /// <summary>
    /// Handle a key press.
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    /// <param name="keySymbol">The character representation (unused for direction input)</param>
    /// <returns>True if handler is done and should be popped, false to continue waiting</returns>
    public bool HandleKey(Key key, string? keySymbol)
    {
        // Map key to direction.
        Direction? direction = KeyToDirection(key);
        
        // Check for cancel.
        if (key == Key.Escape)
        {
            onComplete(null);
            return true;  // Done - cancelled
        }
        
        // Check for valid direction.
        if (direction.HasValue)
        {
            onComplete(direction);
            return true;  // Done - direction selected
        }
        
        // Invalid key - keep waiting
        return false;
    }
    
    /// <summary>
    /// Convert Avalonia Key to Direction enum.
    /// </summary>
    private static Direction? KeyToDirection(Key key)
    {
        return key switch
        {
            // Arrow keys (4-way)
            Key.Up => Direction.North,
            Key.Down => Direction.South,
            Key.Left => Direction.West,
            Key.Right => Direction.East,
            
            // Numpad (8-way + here)
            Key.NumPad7 => Direction.NorthWest,
            Key.NumPad8 => Direction.North,
            Key.NumPad9 => Direction.NorthEast,
            Key.NumPad4 => Direction.West,
            Key.NumPad5 => Direction.Here,
            Key.NumPad6 => Direction.East,
            Key.NumPad1 => Direction.SouthWest,
            Key.NumPad2 => Direction.South,
            Key.NumPad3 => Direction.SouthEast,
            
            // Home/End/PgUp/PgDn as alternative diagonals
            Key.Home => Direction.NorthWest,
            Key.End => Direction.SouthWest,
            Key.PageUp => Direction.NorthEast,
            Key.PageDown => Direction.SouthEast,
            
            // Not a direction key
            _ => null
        };
    }
}
