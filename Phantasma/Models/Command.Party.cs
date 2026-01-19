using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Party - Party Management Commands
/// 
/// Commands for interacting with party members and NPCs:
/// - Ztats: View detailed character stats
/// - NewOrder: Rearrange party member order
/// - SelectPartyMember: Helper for selecting a party member
/// </summary>
public partial class Command
{
    // ===================================================================
    // PARTY MEMBER SELECTION - Helper for other commands
    // ===================================================================
    
    /// <summary>
    /// Select a party member.
    /// 
    /// SIMPLIFIED VERSION: Returns first party member or player.
    /// Full implementation would show status selection UI and wait for input.
    /// </summary>
    /// <returns>Selected character, or null if no party/cancelled</returns>
    protected Character? SelectPartyMember()
    {
        if (session.Party == null)
        {
            return null;
        }
        
        // Save current status mode.
        var oldMode = session.Status?.Mode ?? StatusMode.ShowParty;
        
        // Switch to character selection mode.
        session.Status?.SetMode(StatusMode.SelectCharacter);
        
        // TODO: Full implementation should:
        // 1. Push a CharacterSelectKeyHandler onto the key handler stack
        // 2. Wait for user to select with arrow keys + Enter
        // 3. Pop the handler when done
        // 4. Return the selected character
        
        // SIMPLIFIED VERSION for now:
        // Return first party member (or player if no party).
        Character? selected = null;
        
        if (session.Party.GetSize() > 0)
        {
            selected = session.Party.GetMemberAtIndex(0);
        }
        else if (session.Player != null)
        {
            selected = session.Player;
        }
        
        // Restore old status mode.
        session.Status?.SetMode(oldMode);
        
        return selected;
    }
    
    // ===================================================================
    // ZTATS COMMAND - View detailed character stats
    // ===================================================================
    
    /// <summary>
    /// Ztats Command - view detailed character statistics.
    /// </summary>
    /// <param name="pc">Character to view stats for, or null to prompt</param>
    public void Ztats(Character? pc = null)
    {
        ShowPrompt("Stats-");
        
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                ShowPrompt("Stats-none!");
                return;
            }
        }
        
        ShowPrompt($"Stats-{pc.GetName()}-<ESC to exit>");
        
        // Select this character in status window.
        if (session.Status != null)
        {
            session.Status.SelectedCharacterIndex = pc.Order;
            session.Status.SetMode(StatusMode.Ztats);
        }
        
        Log($"=== {pc.GetName()} ===");
        Log($"Lvl={pc.GetLevel(),3}    XP:{pc.GetExperience(),7}");
        Log($"Str={pc.GetStrength(),3}    HP:{pc.GetHealth(),3}/{pc.GetMaxHp(),3}");
        Log($"Int={pc.GetIntelligence(),3}    MP:{pc.GetMana(),3}/{pc.GetMaxMana(),3}");
        Log($"Dex={pc.GetDexterity(),3}    AC:{pc.ArmorClass,3}");
        
        // TODO: Push scroller key handler for navigating stats.
        // For now, just log that we're showing stats.
        Log($"Viewing stats for {pc.GetName()}");
        
        // Wait for user input (ESC to exit).
        // In full implementation, this would push a key handler.
        
        // Restore normal status mode.
        session.Status?.SetMode(StatusMode.ShowParty);
        ClearPrompt();
    }
    
    // ===================================================================
    // NEW ORDER COMMAND - Rearrange party member order
    // ===================================================================
    
    /// <summary>
    /// NewOrder Command - switch positions of two party members.
    /// </summary>
    public void NewOrder()
    {
        if (session.Party == null)
        {
            return;
        }
        
        int partySize = session.Party.GetSize();
        
        // Handle edge cases.
        switch (partySize)
        {
            case 0:
                return;
            case 1:
                Log("New Order - only one party member!");
                return;
            case 2:
                // Auto-swap if only 2 members.
                var pc1 = session.Party.GetMemberAtIndex(0);
                var pc2 = session.Party.GetMemberAtIndex(1);
                if (pc1 != null && pc2 != null)
                {
                    SwapPartyMembers(pc1, pc2);
                }
                return;
        }
        
        // 3+ members: prompt for two to switch.
        ShowPrompt("Switch-");
        
        // Set status mode BEFORE calling select to avoid screen flash.
        if (session.Status != null)
        {
            session.Status.SetMode(StatusMode.SelectCharacter);
        }
        
        var first = SelectPartyMember();
        if (first == null)
        {
            session.Status?.SetMode(StatusMode.ShowParty);
            return;
        }
        
        ShowPrompt($"Switch-{first.GetName()}-with-");
        
        var second = SelectPartyMember();
        if (second == null)
        {
            session.Status?.SetMode(StatusMode.ShowParty);
            return;
        }
        
        session.Status?.SetMode(StatusMode.ShowParty);
        
        SwapPartyMembers(first, second);
    }
    
    /// <summary>
    /// Helper to swap two party members' positions.
    /// </summary>
    private void SwapPartyMembers(Character pc1, Character pc2)
    {
        if (session.Party == null)
            return;
        
        session.Party.SwitchOrder(pc1, pc2);
        
        Log($"New Order: {pc1.GetName()} switched with {pc2.GetName()}");
        
        // If one was the party leader, make the other the new leader.
        if (pc1.IsLeader && pc2.CanBeLeader)
        {
            session.Party.SetLeader(pc2);
            Log($"{pc2.GetName()} is now the party leader.");
        }
        else if (pc2.IsLeader && pc1.CanBeLeader)
        {
            session.Party.SetLeader(pc1);
            Log($"{pc1.GetName()} is now the party leader.");
        }
    }
}
