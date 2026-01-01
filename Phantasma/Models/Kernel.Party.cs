using System;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-PARTY API IMPLEMENTATIONS
    // These set party properties.
    // ===================================================================
    
    /// <summary>
    /// (kern-party-add-member party character)
    /// Adds a character to a party.
    /// </summary>
    public static object PartyAddMember(object party, object character)
    {
        var group = ResolveObject<Party>(party);
        
        // Fallback: if "player" was passed and not found, try the global player party.
        if (group == null)
        {
            group = Phantasma.GetRegisteredObject(KEY_PLAYER_PARTY) as Party;
        }
        
        var member = ResolveObject<Character>(character);
        
        if (group == null)
        {
            Console.WriteLine("[ERROR] kern-party-add-member: party not found.");
            return false;
        }
        if (member == null)
        {
            Console.WriteLine("[ERROR] kern-party-add-member: character not found.");
            return false;
        }
        
        return group.AddMember(member);
    }
    
    public static object PartySetWandering(object party, object wandering)
    {
        // kern-party-set-wandering <party> <bool>
        // Sets whether a party wanders randomly.
        
        bool isWandering = wandering is bool b ? b : Convert.ToBoolean(wandering);
        var group = party as Party;
        group.IsWandering = isWandering;
        
        return true;
    }
}
