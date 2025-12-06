using System;
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
        var group = party as Party;
        var member = character as Character;

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
