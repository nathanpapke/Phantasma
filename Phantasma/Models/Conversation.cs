using System;
using IronScheme;

namespace Phantasma.Models;

/// <summary>
/// Conversation System - Manages keyword-based dialog with NPCs.
/// 
/// Port of Nazghul's conv.c system.
/// In Nazghul, conversations work through Scheme closures that respond to keywords.
/// The player types keywords like "name", "job", "join", etc.
/// The NPC's conversation closure is called with the keyword and returns a response.
/// 
/// Architecture:
/// 1. Player presses 'T' to talk
/// 2. Selects target NPC
/// 3. Conversation.Enter() is called with NPC and player
/// 4. Loop: Player types keyword → Call NPC's conversation closure → Display response
/// 5. Loop until player types "bye" or cancels
/// </summary>
public class Conversation
{
    private const int MaxKeywordLength = 16;
    private const int KeywordTruncateLength = 4;  // Nazghul truncates to 4 chars
    
    private Session session;
    private Character npc;
    private Character player;
    private object conversationClosure;  // IronScheme Closure
    private bool conversationEnded;
    
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
            conversationClosure = conversationClosure,
            conversationEnded = false
        };
        
        conv.RunWithTracking();
    }
    
    /// <summary>
    /// Main conversation loop.
    /// </summary>
    private void Run()
    {
        // Log conversation start.
        session.LogMessage("*** CONVERSATION ***");
        session.LogMessage($"You meet {npc.GetName()}.");
        
        // Start with "hail" keyword.
        string keyword = "hail";
        
        while (!conversationEnded)
        {
            // Truncate keyword to 4 characters (Nazghul behavior).
            if (keyword.Length > KeywordTruncateLength)
            {
                keyword = keyword.Substring(0, KeywordTruncateLength);
            }
            
            // Execute the conversation closure with the keyword.
            try
            {
                ExecuteConversation(keyword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Conversation] Error executing conversation: {ex.Message}");
                session.LogMessage($"Error in conversation: {ex.Message}");
                break;
            }
            
            if (conversationEnded)
                break;
            
            // Get next keyword from player.
            // For now, we'll use a simple console-based approach.
            // TODO: Replace with proper UI input.
            keyword = GetPlayerKeyword();
            
            if (string.IsNullOrEmpty(keyword))
                keyword = "bye";
            
            // Log what player said.
            session.LogMessage($"{player.GetName()}: {keyword}");
            
            // Check if player wants to end conversation.
            if (keyword.Equals("bye", StringComparison.OrdinalIgnoreCase))
            {
                End();
            }
        }
        
        session.LogMessage("*** END CONVERSATION ***");
    }
    
    /// <summary>
    /// Execute the conversation closure with a keyword.
    /// The closure is a Scheme procedure: (lambda (keyword npc pc) ...)
    /// </summary>
    private void ExecuteConversation(string keyword)
    {
        try
        {
            // Call the Scheme closure with (keyword npc pc).
            // Format: (closure keyword-string npc-character pc-character)
            $"({conversationClosure} \"{keyword}\" #f #f)".Eval();
            
            // The closure should call kern-conv-say to display messages.
            // We don't need to do anything with the result here.
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute conversation closure: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Get keyword input from player.
    /// </summary>
    private string GetPlayerKeyword()
    {
        // TODO: Implement proper UI input system.
        // For now, return empty string which will trigger "bye".
        
        // In full implementation:
        // 1. Display prompt: "Say: "
        // 2. Wait for player to type keyword
        // 3. Return keyword
        
        // For testing, we'll just end the conversation immediately.
        return "bye";
    }
    
    /// <summary>
    /// End the current conversation.
    /// Called by kern-conv-end from Scheme.
    /// </summary>
    public static void End()
    {
        // Set flag to exit conversation loop.
        // Note: This is a static method called from Kernel.cs
        // We'll need to track the active conversation globally.
        
        if (ActiveConversation != null)
        {
            ActiveConversation.conversationEnded = true;
        }
    }
    
    /// <summary>
    /// Track the currently active conversation (for kern-conv-end).
    /// </summary>
    public static Conversation ActiveConversation { get; set; }
    
    /// <summary>
    /// Updated Run() that sets active conversation.
    /// </summary>
    private void RunWithTracking()
    {
        ActiveConversation = this;
        try
        {
            Run();
        }
        finally
        {
            ActiveConversation = null;
        }
    }
}
