using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Special - Special Activity Commands
/// 
/// Commands for special activities:
/// - Camp: Camp in the wilderness (with guard)
/// - Rest: Rest in town (at beds)
/// - Board: Board a vehicle
/// - Exit: Exit a vehicle
/// </summary>
public partial class Command
{
    // ===================================================================
    // CAMP COMMAND - Camp in wilderness
    // ===================================================================
    
    /// <summary>
    /// Camp Command - set up camp in the wilderness.
    /// Mirrors Nazghul's cmd_camp_in_wilderness().
    /// 
    /// Flow:
    /// 1. Check if location is passable for camping
    /// 2. Prompt for hours to sleep
    /// 3. Prompt to set a watch (select guard)
    /// 4. Begin camping mode
    /// 5. Possibly trigger random encounters
    /// </summary>
    /// <param name="camper">Party camping</param>
    public int CampInWilderness(Party camper)
    {
        ShowPrompt("Camp-");
        
        // Check if location is valid for camping.
        if (camper == null || camper.GetPlace() == null)
        {
            ShowPrompt("not here!");
            Log("Camp - not here!");
            return 0;
        }
        
        // TODO: Implement full camping logic.
        // - Check terrain passability.
        // - Check for subplaces (can't camp on towns/dungeons).
        // - Prompt for hours.
        // - Prompt for guard.
        // - Begin camping mode.
        // - Handle random encounters.
        
        Log("Camp command not fully implemented yet");
        Log("Will be implemented in later tasks");
        
        return 0;
    }
    
    // ===================================================================
    // REST COMMAND - Rest at bed in town
    // ===================================================================
    
    /// <summary>
    /// Rest Command - rest at a bed in town.
    /// Mirrors Nazghul's cmd_camp_in_town().
    /// 
    /// Flow:
    /// 1. Check party is in follow mode
    /// 2. Check for bed object at location
    /// 3. Party rendezvous at bed
    /// 4. Prompt for hours to sleep
    /// 5. Begin resting mode
    /// </summary>
    /// <param name="camper">Character initiating rest</param>
    public int RestInTown(Character camper)
    {
        ShowPrompt("Rest-");
        
        // Check party is in follow mode.
        if (session.Party == null)
        {
            return 0;
        }
        
        // TODO: Check party control mode when implemented.
        // if (party.GetPartyControlMode() != PARTY_CONTROL_FOLLOW)
        
        // Check for bed at location.
        var place = camper.GetPlace();
        if (place == null)
        {
            return 0;
        }
        
        var bed = place.GetObjectAt(camper.GetX(), camper.GetY(), ObjectLayer.Bed);
        if (bed == null)
        {
            ShowPrompt("no bed!");
            Log("Camp - no bed here!");
            return 0;
        }
        
        // TODO: Implement full resting logic.
        // - Party rendezvous.
        // - Prompt for hours.
        // - Begin resting mode.
        
        Log("Rest command not fully implemented yet");
        
        return 0;
    }
    
    // ===================================================================
    // VEHICLE COMMANDS - Board and Exit Vehicles
    // ===================================================================
    
    /// <summary>
    /// Board Command - board a vehicle (ship, horse, etc).
    /// </summary>
    public bool Board()
    {
        Log("Board command not yet implemented");
        return false;
    }
    
    /// <summary>
    /// Exit Command - exit current vehicle.
    /// </summary>
    public bool Exit()
    {
        Log("Exit command not yet implemented");
        return false;
    }
}
