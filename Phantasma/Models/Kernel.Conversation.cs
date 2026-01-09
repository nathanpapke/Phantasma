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
    public static object ConversationSay(object[] args)
    {
        Console.WriteLine($"[kern-conv-say] Called with {args.Length} args");
        
        if (args.Length < 1)
        {
            Console.WriteLine("[kern-conv-say] No arguments!");
            return "nil".Eval();
        }
        
        // First arg is speaker.
        var speaker = args[0];
        string speakerName = "???";
        
        if (speaker is Character ch)
            speakerName = ch.GetName();
        else if (speaker is Being b)
            speakerName = b.GetName();
        else if (speaker != null)
            speakerName = speaker.ToString() ?? "???";
        
        // Remaining args are text items to concatenate.
        var sb = new System.Text.StringBuilder();
        sb.Append(speakerName);
        sb.Append(": ");
        
        for (int i = 1; i < args.Length; i++)
        {
            AppendSchemeValue(sb, args[i]);
        }
        
        string message = sb.ToString();
        Console.WriteLine($"[kern-conv-say] Message: {message}");
        
        var session = Phantasma.MainSession;
        session?.LogMessage(message);
        
        return "nil".Eval();
    }

    /// <summary>
    /// Recursively append a Scheme value to a StringBuilder.
    /// Handles Cons lists, strings, numbers, etc.
    /// </summary>
    private static void AppendSchemeValue(System.Text.StringBuilder sb, object value)
    {
        if (value == null)
            return;
        
        // Handle Cons (Scheme list) - iterate through elements
        if (value is Cons cons)
        {
            while (cons != null)
            {
                AppendSchemeValue(sb, cons.car);
                cons = cons.cdr as Cons;
            }
        }
        else if (value is string s)
        {
            sb.Append(s);
        }
        else if (value is int || value is long || value is double || value is float)
        {
            sb.Append(value);
        }
        else
        {
            // For other types, try ToString but log it.
            string str = value.ToString() ?? "";
            if (!string.IsNullOrEmpty(str) && !str.Contains("IronScheme"))
            {
                sb.Append(str);
            }
            else
            {
                Console.WriteLine($"[kern-conv-say] Skipping unknown type: {value.GetType().Name}");
            }
        }
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
        return "nil".Eval();
    }
    
    /// <summary>
    /// (kern-conv-end)
    /// End the current conversation.
    /// </summary>
    public static object ConversationEnd()
    {
        Console.WriteLine("[Conversation] Ending conversation.");
        Conversation.End();
        return "nil".Eval();
    }
}
