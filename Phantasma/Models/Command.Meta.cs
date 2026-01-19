using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Meta - Meta Game Commands
/// 
/// Commands for game control and information:
/// - Quit: Quit and save the game
/// - QuickSave: Quick save
/// - Reload: Quick load
/// - Help: Show help/commands
/// - ShowInfo: Show current location/time info (AT command)
/// </summary>
public partial class Command
{
    // ===================================================================
    // QUIT COMMAND - Exit game
    // ===================================================================
    
    /// <summary>
    /// Quit Command - quit game with save prompt.
    /// 
    /// Flow:
    /// 1. Show "Quit & Save Game-Y/N?" prompt
    /// 2. Wait for Y/N input
    /// 3. If yes, save and set quit flag
    /// </summary>
    public void Quit()
    {
        ShowPrompt("Quit & Save Game-<Y/N>?");
        
        // Request yes/no confirmation
        RequestYesNo(confirmed => CompleteQuit(confirmed));
    }
    
    /// <summary>
    /// Complete the Quit command after confirmation received.
    /// </summary>
    private void CompleteQuit(bool confirmed)
    {
        if (confirmed)
        {
            ShowPrompt("Yes!");
            Log("Goodbye!");
            
            // Save the game.
            session.Save("quicksave.scm");
            
            // Signal quit
            // TODO: Set a quit flag that the game loop checks
            // For now, just log
            Console.WriteLine("[Quit] Quit confirmed, game saved");
            
            // Close the application.
            // This would typically be handled by the main window.
        }
        else
        {
            ShowPrompt("Quit & Save Game-No");
        }
    }
    
    // ===================================================================
    // SAVE/LOAD COMMANDS
    // ===================================================================
    
    /// <summary>
    /// QuickSave Command - save to quicksave slot.
    /// </summary>
    public void QuickSave()
    {
        Log("Saving to quicksave.scm...");
        session.Save("quicksave.scm");
        Log("ok!");
    }
    
    /// <summary>
    /// Reload Command - load from quicksave slot.
    /// </summary>
    public void Reload()
    {
        Log("Loading from quicksave.scm...");
        session.Load("quicksave.scm");
        Log("ok!");
    }
    
    // ===================================================================
    // HELP COMMAND
    // ===================================================================
    
    /// <summary>
    /// Help Command - show help text with available commands.
    /// </summary>
    public void Help()
    {
        // TODO: Implement full help system.
        // - Show Page mode in status window.
        // - Display scrollable help text.
        // - List all available commands and keys.
        
        Log("=== PHANTASMA COMMANDS ===");
        Log("Arrow Keys / Numpad: Move");
        Log("G: Get items");
        Log("D: Drop items");
        Log("T: Talk to NPCs");
        Log("Z: View character stats");
        Log("O: Open containers/doors");
        Log("H: Handle mechanisms");
        Log("U: Use items");
        Log("R: Ready/equip weapons");
        Log("C: Cast spells");
        Log("M: Mix reagents");
        Log("A: Attack");
        Log("N: New order (swap party positions)");
        Log("S: Search");
        Log("@: Show location info");
        Log("F5: Quick save");
        Log("F9: Quick load");
        Log("?: Help");
        Log("");
    }
    
    // ===================================================================
    // SHOW INFO COMMAND - Display current game state
    // ===================================================================
    
    /// <summary>
    /// ShowInfo (@) Command - display current location, time, and game state.
    /// </summary>
    public void ShowInfo()
    {
        ShowPrompt("@-");
        
        var place = session.CurrentPlace;
        var player = session.Player;
        
        if (place == null || player == null)
        {
            Log("No location info available");
            return;
        }
        
        Log($"=== LOCATION INFO ===");
        Log($"Place: {place.Name}");
        Log($"Position: ({player.GetX()}, {player.GetY()})");
        
        // Time info
        var clock = session.Clock;
        if (clock != null)
        {
            Log($"Time: {clock.Hour}:{clock.Min}");
            Log($"Day: {clock.Day}");
        }
        
        // Weather/wind
        var wind = session.Wind;
        if (wind != null)
        {
            Log($"Wind: {wind.DirectionString}");
        }
        
        // Terrain at player position
        var terrain = place.GetTerrain(player.GetX(), player.GetY());
        if (terrain != null)
        {
            Log($"Terrain: {terrain.Name}");
        }
        
        ClearPrompt();
    }
}
