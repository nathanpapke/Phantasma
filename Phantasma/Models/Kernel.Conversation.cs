using System;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-CONV API IMPLEMENTATIONS
    // Conversation functions for keyword-based dialog.
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-say speaker text)
    /// NPC speaks a line of dialog to the player.
    /// </summary>
    public static object ConversationSay(object speaker, object text)
    {
        string message = text?.ToString() ?? "";
        var session = Phantasma.MainSession;
        session?.LogMessage(message);
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-conv-get-reply pc)
    /// Get a keyword reply from the player.
    /// Returns a symbol representing the keyword (truncated to 4 chars).
    /// </summary>
    public static object ConversationGetReply(object pc)
    {
        // TODO: Implement proper UI input.
        // For now, just return 'bye to end conversation.
        Console.WriteLine("[Conversation] Getting player reply (returning 'bye for now)");
        return "bye".Eval();
    }
    
    /// <summary>
    /// (kern-conv-get-yes-no pc)
    /// Prompt player for yes/no response.
    /// Returns #t for yes, #f for no.
    /// </summary>
    public static object ConversationGetYesNo(object pc)
    {
        // TODO: Implement UI prompt.
        Console.WriteLine("[Conversation] Yes/No prompt (returning #f for now)");
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-conv-get-amount pc)
    /// Prompt player for a numeric amount.
    /// Returns the number entered.
    /// </summary>
    public static object ConversationGetAmount(object pc)
    {
        // TODO: Implement UI prompt.
        Console.WriteLine("[Conversation] Amount prompt (returning 0 for now)");
        return 0;
    }
    
    /// <summary>
    /// (kern-conv-trade npc pc trade-list)
    /// Handle merchant trading interface.
    /// </summary>
    public static object ConversationTrade(object npc, object pc, object tradeList)
    {
        // TODO: Implement trading system.
        Console.WriteLine("[Conversation] Trade interface (not yet implemented)");
        return Builtins.Unspecified;
    }
    
    /// <summary>
    /// (kern-conv-end)
    /// End the current conversation.
    /// </summary>
    public static object ConversationEnd()
    {
        Console.WriteLine("[Conversation] Ending conversation.");
        Conversation.End();
        return Builtins.Unspecified;
    }
}
