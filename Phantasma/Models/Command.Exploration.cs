using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Exploration - Exploration and Examination Commands
/// 
/// Commands for examining the game world:
/// - Xamine: Look/examine mode (move cursor to look at things)
/// - LookAt: Look at a specific location
/// </summary>
public partial class Command
{
    // ===================================================================
    // XAMINE COMMAND - Interactive examination mode
    // ===================================================================
    
    /// <summary>
    /// Xamine Command - enter examination mode with cursor.
    /// Mirrors Nazghul's cmdXamine().
    /// 
    /// Flow:
    /// 1. Show current tile description
    /// 2. Push targeting handler
    /// 3. Player moves cursor with arrow keys
    /// 4. Each tile is described as cursor moves
    /// 5. ESC to exit mode
    /// </summary>
    /// <param name="pc">Character examining, or null to use player</param>
    public bool Xamine(Character? pc = null)
    {
        if (pc == null)
        {
            pc = session.Player;
        }
        
        if (pc == null)
        {
            return false;
        }
        
        ShowPrompt("Xamine-");
        
        int x = pc.GetX();
        int y = pc.GetY();
        
        LogBeginGroup();
        
        Log($"{pc.GetName()} examines around...");
        
        // Look at current tile first.
        LookAtXY(pc.GetPlace(), x, y);
        
        // TODO: Implement full examination mode.
        // - Push targeting handler with callback.
        // - Call LookAtXY for each tile cursor moves to.
        // - Allow detailed examination with another key.
        
        Log("Xamine mode not fully implemented yet");
        
        LogEndGroup();
        
        return true;
    }
    
    // ===================================================================
    // LOOK AT HELPERS
    // ===================================================================
    
    /// <summary>
    /// Look at a specific location and describe what's there.
    /// Helper for Xamine and other commands.
    /// </summary>
    private void LookAtXY(Place? place, int x, int y)
    {
        if (place == null)
        {
            return;
        }
        
        Log($"At XY=({x},{y}) you see:");
        
        // TODO: Implement full description.
        // - Check if tile is visible (LOS).
        // - Describe terrain.
        // - Describe objects.
        // - Describe beings.
        
        Log("  (description not yet implemented)");
    }
    
    /// <summary>
    /// Detailed examination of a location.
    /// Shows technical info like passability, LOS blocking, etc.
    /// </summary>
    private void DetailedExamineXY(Place? place, int x, int y)
    {
        if (place == null)
        {
            return;
        }
        
        // TODO: Implement detailed examination.
        // - Show all objects on tile.
        // - Show passability info.
        // - Show LOS blocking info.
        // - Show trigger/mechanism info.
        // - Show any special properties.
        
        Log($"Detailed examination of ({x},{y}) not yet implemented");
    }
}
