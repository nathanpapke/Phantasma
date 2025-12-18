using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Movement - Movement and Search Commands
/// 
/// Commands related to character movement:
/// - Search: Search for hidden objects/doors in a direction
/// - Movement helpers and utilities
/// </summary>
public partial class Command
{
    // ===================================================================
    // SEARCH COMMAND - Find hidden objects
    // ===================================================================
    
    /// <summary>
    /// Search Command - search for hidden objects in a direction.
    /// 
    /// Flow:
    /// 1. Prompt for direction
    /// 2. Temporarily reveal hidden objects in that location
    /// 3. Describe what was found
    /// </summary>
    /// <param name="place">Place to search in</param>
    /// <param name="x">Starting X coordinate</param>
    /// <param name="y">Starting Y coordinate</param>
    public bool Search(Place place, int x, int y)
    {
        ShowPrompt("Search-");
        
        var dir = PromptForDirection();
        if (dir == null)
        {
            return false;
        }

        int dx = Common.DirectionToDx(dir);
        int dy = Common.DirectionToDy(dir);
        
        int targetX = x + dx;
        int targetY = y + dy;
        
        // TODO: Implement search logic.
        // - Temporarily enable Reveal mode.
        // - Describe everything at target location.
        // - Restore Reveal mode.
        
        Log($"Search not fully implemented yet");
        
        return true;
    }
}
