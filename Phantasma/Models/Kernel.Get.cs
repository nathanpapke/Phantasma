using System;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-GET API IMPLEMENTATIONS
    // ===================================================================
    
    public static object GetPlayer()
    {
        // (kern-get-player)
        // Returns the player character from the main session.
        
        try
        {
            var session = Phantasma.MainSession;
            if (session != null && session.Player != null)
            {
                return session.Player;
            }
            else
            {
                RuntimeError("kern-get-player: No active session or player");
                return Builtins.Unspecified;
            }
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-get-player: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
}
