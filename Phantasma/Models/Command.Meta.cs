using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Meta - Meta Game Commands
/// 
/// Commands for game control and information:
/// - Quit: Quit and save the game
/// - Save: Quick save
/// - Load: Quick load
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
    /// </summary>
    public bool Quit()
    {
        ShowPrompt("Quit & Save Game-Y/N?");
        
        bool saveAndQuit = PromptForYesNo("Quit & Save Game");
        
        if (saveAndQuit)
        {
            ShowPrompt("Yes!");
            Log("Goodbye!");
            session.Save("quicksave.scm");
            
            // Set quit flag.
            // TODO: Implement proper quit handling.
            return true;
        }
        else
        {
            ShowPrompt("No");
            return false;
        }
    }
    
    // ===================================================================
    // SAVE/LOAD COMMANDS
    // ===================================================================
    
    /// <summary>
    /// QuickSave Command - save to quicksave slot.
    /// Mirrors Nazghul's cmdQuickSave().
    /// </summary>
    public void QuickSave()
    {
        Log("Saving to quicksave.scm...");
        session.Save("quicksave.scm");
        Log("ok!");
    }
    
    /// <summary>
    /// Reload Command - load from quicksave slot.
    /// Mirrors Nazghul's cmdReload().
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
    /// Mirrors Nazghul's cmdHelp().
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
        Log("T: Talk to NPCs");
        Log("Z: View character stats");
        Log("O: Open containers/doors");
        Log("U: Use items");
        Log("R: Ready/equip weapons");
        Log("C: Cast spells (Task 17)");
        Log("M: Mix reagents (Task 17)");
        Log("A: Attack");
        Log("N: New order (swap party positions)");
        Log("F5: Quick save");
        Log("F9: Quick load");
        Log("?: Help");
        Log("");
    }
    
    // ===================================================================
    // SHOW INFO COMMAND - Display current game state
    // ===================================================================
    
    /// <summary>
    /// ShowInfo Command - display current location, time, and game state.
    /// Mirrors Nazghul's cmdAT() (the @ command).
    /// </summary>
    public void ShowInfo()
    {
        var player = session.Player;
        var place = session.CurrentPlace;
        
        if (player == null || place == null)
        {
            return;
        }
        
        LogBeginGroup();
        
        // Show location info.
        string who = session.Party != null ? "The party" : player.GetName();
        string placeName = place.Name ?? "Unknown";
        int x = player.GetX();
        int y = player.GetY();
        
        Log($"{who} is in {placeName} ({x},{y}).");
        
        // TODO: Show time/date info when clock system is implemented.
        // Log($"It is {time} on {date}.");
        
        // TODO: Show turn count when implemented.
        // Log($"{turnCount} game turns have passed.");
        
        // TODO: Show wind direction when implemented.
        // Log($"The wind is blowing from the {direction}.");
        
        LogEndGroup();
    }
}
