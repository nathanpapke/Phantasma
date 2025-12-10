using System;

namespace Phantasma.Models;

/// <summary>
/// Command.Party - Party Management Commands
/// 
/// Commands for interacting with party members and NPCs:
/// - Talk: Start conversations with NPCs
/// - Ztats: View detailed character stats
/// - NewOrder: Rearrange party member order
/// </summary>
public partial class Command
{
    // ===================================================================
    // TALK COMMAND - Initiate conversations with NPCs
    // ===================================================================
    
    /// <summary>
    /// Start a conversation with an NPC.
    /// </summary>
    /// <param name="member">Party member doing the talking, or null to prompt</param>
    public bool Talk(Character? member = null)
    {
        if (session.Player == null || session.CurrentPlace == null)
        {
            Log("Talk - no player or place");
            return false;
        }
        
        ShowPrompt("Talk-");
        
        // Select party member if not provided.
        if (member == null)
        {
            member = SelectPartyMember();
            if (member == null)
            {
                return false;
            }
        }
        
        int x = member.GetX();
        int y = member.GetY();
        
        // TODO: Implement full targeting for talk.
        // For now, check adjacent tiles for NPCs.
        // Full implementation uses select_target(x, y, &x, &y, range).
        
        // Check all 8 adjacent directions for an NPC.
        int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };
        
        Object? target = null;
        for (int i = 0; i < 8; i++)
        {
            int checkX = x + dx[i];
            int checkY = y + dy[i];
            
            var being = session.CurrentPlace.GetBeingAt(checkX, checkY);
            if (being != null && being != member)
            {
                target = being;
                break;
            }
        }
        
        if (target == null)
        {
            ShowPrompt("nobody there!");
            Log("Try talking to a PERSON.");
            return false;
        }
        
        // Get the actual speaker (in case target is a party).
        var speaker = target.GetSpeaker() as Character;
        if (speaker == null)
        {
            ShowPrompt("cancel");
            return false;
        }
        
        // Check if speaker has a conversation.
        var conv = speaker.Conversation;
        if (conv == null)
        {
            ShowPrompt("no response!");
            Log($"No response from {speaker.GetName()}.");
            return true;
        }
        
        ShowPrompt(speaker.GetName());
        
        Log("*** CONVERSATION ***");
        Log($"You meet {speaker.GetName()}.");
        
        // Check if NPC is sleeping.
        if (speaker.GetActivity() == Activity.Sleeping)
        {
            Log("Zzzz...");
            return true;
        }
        
        // Start the conversation.
        // This pushes conversation handlers onto the key handler stack.
        Conversation.Enter(session, speaker, member, conv);
        
        // TODO: mapSetDirty() when needed
        
        return true;
    }
    
    // ===================================================================
    // ZTATS COMMAND - View detailed character stats
    // ===================================================================
    
    /// <summary>
    /// Ztats Command - view detailed character statistics.
    /// Mirrors Nazghul's cmdZtats().
    /// </summary>
    /// <param name="pc">Character to view stats for, or null to prompt</param>
    public bool Ztats(Character? pc = null)
    {
        ShowPrompt("Stats-");
        
        if (pc == null)
        {
            pc = SelectPartyMember();
            if (pc == null)
            {
                return false;
            }
        }
        
        ShowPrompt(pc.GetName() + "-<ESC to exit>");
        
        // Select this character in status window.
        if (session.Status != null)
        {
            session.Status.SelectedCharacterIndex = pc.Order;
            session.Status.SetMode(StatusMode.Ztats);
        }
        
        // TODO: Push scroller key handler for navigating stats.
        // For now, just log that we're showing stats.
        Log($"Viewing stats for {pc.GetName()}");
        
        // Wait for user input (ESC to exit).
        // In full implementation, this would push a key handler.
        
        // Restore normal status mode.
        session.Status?.SetMode(StatusMode.ShowParty);
        ClearPrompt();
        ShowPrompt("ok");
        
        return false;
    }
    
    // ===================================================================
    // NEW ORDER COMMAND - Rearrange party member order
    // ===================================================================
    
    /// <summary>
    /// NewOrder Command - switch positions of two party members.
    /// Mirrors Nazghul's cmdNewOrder().
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
        
        ShowPrompt("-with-");
        
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
            SwitchPartyLeader(pc1, pc2);
        }
        else if (pc2.IsLeader && pc1.CanBeLeader)
        {
            SwitchPartyLeader(pc2, pc1);
        }
        
        // Repaint status window to show new order.
        // TODO: Trigger status repaint when implemented.
    }
    
    /// <summary>
    /// Helper to switch party leadership between two characters.
    /// </summary>
    private void SwitchPartyLeader(Character oldLeader, Character newLeader)
    {
        if (session.Party == null)
            return;
        
        session.Party.SetLeader(newLeader);
        
        Log($"{newLeader.GetName()} is now the party leader.");
    }
    
    /// <summary>
    /// Select a party member interactively.
    /// Mirrors Nazghul's select_party_member().
    /// </summary>
    /// <returns>Selected character, or null if cancelled</returns>
    protected Character? SelectPartyMember()
    {
        if (session.Party == null)
        {
            ShowPrompt("none!");
            return null;
        }
    
        // Save current status mode.
        var oldMode = session.Status?.Mode ?? StatusMode.ShowParty;
    
        // Switch to character selection mode.
        session.Status?.SetMode(StatusMode.SelectCharacter);
    
        // Show selection prompt
        ShowPrompt("<select>");
    
        // TODO: Full implementation should:
        // 1. Push a CharacterSelectKeyHandler onto the key handler stack
        // 2. Wait for user to select with arrow keys + Enter
        // 3. Pop the handler when done
        // 4. Return the selected character
    
        // SIMPLIFIED VERSION for now:
        // Just return the first party member (or player character).
        Character? selected = null;
    
        if (session.Party.GetSize() > 0)
        {
            // Return first party member.
            selected = session.Party.GetMemberAtIndex(0);
        }
        else if (session.Player != null)
        {
            // Fallback to player if no party.
            selected = session.Player;
        }
    
        // Restore old status mode.
        session.Status?.SetMode(oldMode);
    
        // Update prompt with result.
        if (selected != null)
        {
            ShowPrompt(selected.GetName());
        }
        else
        {
            ShowPrompt("none!");
        }
    
        return selected;
    }
}
