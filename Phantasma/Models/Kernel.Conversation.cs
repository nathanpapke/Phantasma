using System;
using System.Text;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

/*
 *  These functions run on a background thread and BLOCK waiting for input.
 *  This is safe because the UI thread remains responsive.
 *  
 *  To convert to async in a future project, simply:
 *  1. Change methods to async
 *  2. Change .Result to await
 *  
 *  Example:
 *    Current:  var answer = ConversationAsync.RequestYesNoAsync().Result;
 *    Future:   var answer = await ConversationAsync.RequestYesNoAsync();
 */

public partial class Kernel
{
    // ===================================================================
    // KERN-CONV-SAY - NPC speaks dialog
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-say speaker text ...)
    /// NPC speaks a line of dialog.
    /// Thread-safe: marshals to UI thread.
    /// </summary>
    public static object ConversationSay(object[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("[kern-conv-say] No arguments!");
            return "nil".Eval();
        }
        
        // First arg is speaker
        string speakerName = GetSpeakerName(args[0]);
        
        // Remaining args are text to concatenate
        var sb = new StringBuilder();
        for (int i = 1; i < args.Length; i++)
        {
            AppendSchemeValue(sb, args[i]);
        }
        
        string message = $"{speakerName}: {sb}";
        Console.WriteLine($"[kern-conv-say] {message}");
        
        // Thread-safe logging
        ConversationAsync.LogToGame(message);
        
        return "nil".Eval();
    }
    
    private static string GetSpeakerName(object speaker)
    {
        // Handle variadic array wrapper from IronScheme.
        if (speaker is object[] arr && arr.Length > 0)
            speaker = arr[0];
        
        if (speaker is Character ch)
            return ch.GetName();
        if (speaker is Being b)
            return b.GetName();
        return speaker?.ToString() ?? "???";
    }

    private static void AppendSchemeValue(StringBuilder sb, object value)
    {
        if (value == null)
            return;
        
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
            string str = value.ToString() ?? "";
            if (!string.IsNullOrEmpty(str) && !str.Contains("IronScheme"))
            {
                sb.Append(str);
            }
        }
    }
    
    // ===================================================================
    // KERN-CONV-GET-REPLY - Keyword input (BLOCKS)
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-get-reply pc)
    /// Get a keyword reply from the player.
    /// BLOCKS the background thread until input is received.
    /// Returns a symbol (truncated to 4 chars).
    /// </summary>
    public static object ConversationGetReply(object[] args)
    {
        var pc = args.Length > 0 ? args[0] : null;
        
        Console.WriteLine("[kern-conv-get-reply] Blocking for input...");
        
        string reply = ConversationAsync.RequestReplyAsync().Result;
        
        Console.WriteLine($"[kern-conv-get-reply] Got: '{reply}'");
        return SymbolTable.StringToObject(reply);
    }
    
    // ===================================================================
    // KERN-CONV-GET-YES-NO? - Yes/No prompt (BLOCKS)
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-get-yes-no? pc)
    /// Prompt player for yes/no response.
    /// BLOCKS the background thread until input is received.
    /// Returns #t for yes, #f for no.
    /// </summary>
    public static object ConversationGetYesNo(object[]args)
    {
        var pc = args.Length > 0 ? args[0] : null;
        
        Console.WriteLine("[kern-conv-get-yes-no?] Blocking for input...");
        
        // BLOCKING call - safe because we're on background thread
        bool answer = ConversationAsync.RequestYesNoAsync().Result;
        
        Console.WriteLine($"[kern-conv-get-yes-no?] Got: {answer}");
        return answer ? "#t".Eval() : "#f".Eval();
    }
    
    // ===================================================================
    // KERN-CONV-GET-AMOUNT - Numeric input (BLOCKS)
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-get-amount pc)
    /// Prompt player for a numeric amount.
    /// BLOCKS the background thread until input is received.
    /// </summary>
    public static object ConversationGetAmount(object[] args)
    {
        var pc = args.Length > 0 ? args[0] : null;
        
        Console.WriteLine("[kern-conv-get-amount] Blocking for input...");
        
        int amount = ConversationAsync.RequestAmountAsync().Result;
        
        Console.WriteLine($"[kern-conv-get-amount] Got: {amount}");
        return amount;
    }
    
    // ===================================================================
    // KERN-CONV-TRADE - Trading interface
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-trade npc pc trade-list)
    /// Handle merchant trading interface.
    /// </summary>
    public static object ConversationTrade(object[] args)
    {
        var npc = args.Length > 0 ? args[0] : null;
        var pc = args.Length > 1 ? args[1] : null;
        var tradeList = args.Length > 2 ? args[2] : null;
        
        Console.WriteLine("[kern-conv-trade] Trade requested");
        
        // TODO: Implement trade UI
        // For now, just show a message
        ConversationAsync.LogToGame("(Trading not yet implemented)");
        
        return "nil".Eval();
    }
    
    // ===================================================================
    // KERN-CONV-END - End conversation
    // ===================================================================
    
    /// <summary>
    /// (kern-conv-end)
    /// End the current conversation.
    /// </summary>
    public static object ConversationEnd(object[] args)
    {
        Console.WriteLine("[kern-conv-end] Ending conversation");
        Conversation.End();
        return "nil".Eval();
    }
}
