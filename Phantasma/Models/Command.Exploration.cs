using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Exploration - Exploration and Examination Commands
/// 
/// Commands for examining the game world:
/// - Search: Search in a direction
/// - Xamine: Look/examine mode (move cursor to look at things)
/// - LookAt: Look at a specific location
/// </summary>
public partial class Command
{
    // ===================================================================
    // SEARCH COMMAND - Search in a direction (Event-Driven)
    // ===================================================================
    
    /// <summary>
    /// Search Command - search in a direction for hidden items/secrets.
    /// Mirrors Nazghul's cmdSearch().
    /// 
    /// Flow:
    /// 1. Show "Search-" prompt
    /// 2. Wait for direction input
    /// 3. Describe what's found (reveals hidden things)
    /// </summary>
    public void Search()
    {
        ShowPrompt("Search-<direction>");
        
        // Request direction.
        RequestDirection(dir => CompleteSearch(dir));
    }
    
    /// <summary>
    /// Complete the Search command after direction is received.
    /// </summary>
    private void CompleteSearch(Direction? dir)
    {
        if (dir == null)
        {
            ShowPrompt("Search-none!");
            return;
        }
        
        ShowPrompt($"Search-{DirectionToString(dir.Value)}");
        
        var player = session.Player;
        var place = session.CurrentPlace;
        
        if (player == null || place == null)
        {
            return;
        }
        
        int dx = Common.DirectionToDx(dir.Value);
        int dy = Common.DirectionToDy(dir.Value);
        int x = place.WrapX(player.GetX() + dx);
        int y = place.WrapY(player.GetY() + dy);
        
        Log("You find:");
        
        // Enable reveal mode temporarily to show hidden things.
        // TODO: Implement reveal flag like Nazghul's old_reveal/Reveal
        
        DescribeLocation(place, x, y, describeAll: true);
        
        ClearPrompt();
    }
    
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
    public void Xamine(Character? pc = null)
    {
        if (pc == null)
        {
            pc = session.Player;
        }
        
        if (pc == null)
        {
            return;
        }
        
        ShowPrompt("Xamine-");
        
        int x = pc.GetX();
        int y = pc.GetY();
        var place = pc.GetPlace();
        
        LogBeginGroup();
        Log($"{pc.GetName()} examines around...");
        
        // Look at current tile first.
        DescribeLocation(place, x, y, describeAll: false);
        
        // TODO: Implement full examination mode.
        // - Push targeting handler with callback.
        // - Call LookAtXY for each tile cursor moves to.
        // - Allow detailed examination with another key.
        
        LogEndGroup();
    }
    
    // ===================================================================
    // LOOK AT HELPERS
    // ===================================================================
    
    /// <summary>
    /// Describe a location - terrain, objects, beings.
    /// Mirrors Nazghul's place_describe().
    /// </summary>
    /// <param name="place">The place to examine</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="describeAll">If true, include hidden things (Search mode)</param>
    private void DescribeLocation(Place? place, int x, int y, bool describeAll)
    {
        if (place == null)
        {
            Log("  (nothing)");
            return;
        }
        
        bool foundAnything = false;
        
        // Describe terrain.
        var terrain = place.GetTerrain(x, y);
        if (terrain != null)
        {
            Log($"  {terrain.Name}");
            foundAnything = true;
        }
        
        // Describe objects on all layers.
        foreach (ObjectLayer layer in Enum.GetValues(typeof(ObjectLayer)))
        {
            var obj = place.GetObjectAt(x, y, layer);
            if (obj != null)
            {
                // Skip hidden objects unless we're searching.
                if (!describeAll && !obj.IsVisible())
                {
                    continue;
                }
                
                string desc = !obj.IsVisible() ? $"  {obj.Name} (hidden!)" : $"  {obj.Name}";
                Log(desc);
                foundAnything = true;
            }
        }
        
        // Describe beings.
        var being = place.GetBeingAt(x, y);
        if (being != null)
        {
            Log($"  {being.GetName()}");
            foundAnything = true;
        }
        
        if (!foundAnything)
        {
            Log("  (nothing)");
        }
    }
    
    /// <summary>
    /// Look at current position (shortcut).
    /// </summary>
    public void LookHere()
    {
        var player = session.Player;
        var place = session.CurrentPlace;
        
        if (player == null || place == null)
        {
            return;
        }
        
        Log($"At ({player.GetX()}, {player.GetY()}):");
        DescribeLocation(place, player.GetX(), player.GetY(), describeAll: false);
    }
}
