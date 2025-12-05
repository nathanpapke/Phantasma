using System;
using Avalonia.Threading;
using IronScheme.Runtime;

namespace Phantasma.Models;

/// <summary>
/// Conversation System - Keyword-based dialog with NPCs.
/// 
/// Flow:
/// 1. Player presses 'T' to talk, selects adjacent NPC
/// 2. Conversation.Enter() is called
/// 3. Process "hail" keyword (NPC greets player)
/// 4. Push TextInputHandler onto stack
/// 5. Player types keyword, presses Enter
/// 6. TextInputHandler callback processes keyword
/// 7. Repeat until "bye" or kern-conv-end is called
/// </summary>
public class Conversation
{
    private const int KeywordTruncateLength = 4;  // Nazghul truncates to 4 chars
    
    private Session session;
    private Character npc;
    private Character player;
    private object conversationClosure;
    
    /// <summary>
    /// Currently active conversation (for kern-conv-end to find).
    /// </summary>
    public static Conversation Active { get; private set; }
    
    /// <summary>
    /// Enter conversation mode with an NPC.
    /// </summary>
    public static void Enter(Session session, Character npc, Character player, object conversationClosure)
    {
        if (session == null || npc == null || player == null || conversationClosure == null)
        {
            Console.WriteLine("[Conversation] Invalid parameters for conversation");
            return;
        }
        
        var conv = new Conversation
        {
            session = session,
            npc = npc,
            player = player,
            conversationClosure = conversationClosure
        };
        
        Active = conv;
        
        // Log conversation start.
        session.LogMessage("*** CONVERSATION ***");
        session.LogMessage($"You meet {npc.GetName()}.");
        
        // Execute with "hail" keyword first (Nazghul behavior).
        conv.ExecuteKeyword("hail");
        
        // If conversation wasn't ended by "hail", prompt for input.
        if (Active != null)
        {
            conv.PromptForKeyword();
        }
    }
    
    /// <summary>
    /// Execute the conversation closure with a keyword.
    /// </summary>
    private void ExecuteKeyword(string keyword)
    {
        // Truncate to 4 characters (Nazghul behavior).
        if (keyword.Length > KeywordTruncateLength)
        {
            keyword = keyword.Substring(0, KeywordTruncateLength);
        }
        
        try
        {
            // Call the Scheme closure with (keyword npc pc).
            if (conversationClosure is Callable callable)
            {
                callable.Call(keyword, npc, player);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Conversation] Error executing keyword '{keyword}': {ex.Message}");
            session.LogMessage($"Error in conversation: {ex.Message}");
            End();
        }
    }
    
    /// <summary>
    /// Prompt player for next keyword by pushing a TextInputHandler.
    /// </summary>
    private void PromptForKeyword()
    {
        // Show prompt in command window.
        session.SetCommandPrompt("Say: ");
        
        // Create handler that will process the input.
        var handler = new TextInputHandler(
            onComplete: OnKeywordEntered,
            onTextChanged: text => session.UpdateCommandInput(text)
        );
        
        // Push onto stack - MainWindow will route keys here.
        session.PushKeyHandler(handler);
    }
    
    /// <summary>
    /// Called when player finishes typing a keyword.
    /// </summary>
    private void OnKeywordEntered(string input)
    {
        string keyword = string.IsNullOrWhiteSpace(input) ? "bye" : input.ToLower().Trim();
    
        // Log what player said.
        session.LogMessage($"{player.GetName()}: {keyword}");
    
        // Clear the command prompt.
        session.SetCommandPrompt("");
    
        // Execute the keyword.
        ExecuteKeyword(keyword);
    
        // If conversation still active, prompt again.
        // Use Dispatcher.Post to defer until AFTER current handler is popped.
        if (Active != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => PromptForKeyword());
        }
    }
    
    /// <summary>
    /// End the current conversation.
    /// Called by kern-conv-end from Scheme.
    /// </summary>
    public static void End()
    {
        if (Active != null)
        {
            Active.session.SetCommandPrompt("");
            Active.session.LogMessage("*** END CONVERSATION ***");
            Active = null;
        }
    }
}
