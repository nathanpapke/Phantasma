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
    public static object PartyAddMember(object[] args)
    {
        var party = args.Length > 0 ? args[0] : null;
        var character = args.Length > 1 ? args[1] : null;
        
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
    
    /// <summary>
    /// (kern-party-set-wandering party wandering)
    /// Sets whether a party wanders randomly.
    /// </summary>
    public static object PartySetWandering(object[] args)
    {
        var party = args.Length > 0 ? args[0] : null;
        var wandering = args.Length > 1 ? args[1] : null;
        
        bool isWandering = wandering is bool b ? b : Convert.ToBoolean(wandering);
        var group = party as Party;
        group.IsWandering = isWandering;
        
        return true;
    }
}
