using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using IronScheme;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

/*
 *  Threaded Conversation System.
 *  
 *  Runs the Scheme conversation on a background thread, allowing blocking
 *  input calls (RequestYesNoAsync().Result) without freezing the UI.
 *  
 *  This design supports future conversion to async/await by simply:
 *  1. Remove Task.Run wrapper
 *  2. Change .Result calls to await
 *  
 *  Thread Safety:
 *  - Scheme execution: Background thread
 *  - UI updates (LogMessage, prompts): Marshaled to UI thread
 *  - Key handlers: Run on UI thread, signal background thread via TaskCompletionSource
 */

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
    private Session session;
    private Character npc;
    private Character player;
    private object conversationClosure;
    
    /// <summary>
    /// Currently active conversation.
    /// </summary>
    public static Conversation? Active { get; private set; }
    
    /// <summary>
    /// Cancellation token for aborting conversations.
    /// </summary>
    private CancellationTokenSource? cancellationSource;
    
    /// <summary>
    /// The thread running the conversation (for debugging).
    /// </summary>
    private Thread? conversationThread;
    
    /// <summary>
    /// Enter conversation mode with an NPC.
    /// Spawns a background thread to run the conversation.
    /// </summary>
    public static void Enter(Session session, Character npc, Character player, object conversationClosure)
    {
        if (session == null || npc == null || player == null || conversationClosure == null)
        {
            Console.WriteLine("[Conversation] Invalid parameters");
            return;
        }
        
        // End any existing conversation.
        if (Active != null)
        {
            Console.WriteLine("[Conversation] Ending previous conversation");
            Active.Cancel();
        }
        
        // Resolve symbol to closure if needed.
        if (conversationClosure is SymbolId symbolId)
        {
            var symbolName = SymbolTable.IdToString(symbolId);
            Console.WriteLine($"[Conversation] Resolving symbol '{symbolName}'...");
            conversationClosure = symbolName.Eval();
        }
        
        if (conversationClosure is not Callable)
        {
            Console.WriteLine($"[Conversation] ERROR: Not callable: {conversationClosure?.GetType()}");
            session.LogMessage($"Error: {npc.GetName()} has no valid conversation.");
            return;
        }
        
        var conv = new Conversation
        {
            session = session,
            npc = npc,
            player = player,
            conversationClosure = conversationClosure,
            cancellationSource = new CancellationTokenSource()
        };
        
        Active = conv;
        
        // Log conversation start on UI thread.
        ConversationAsync.LogToGame("*** CONVERSATION ***");
        ConversationAsync.LogToGame($"You meet {npc.GetName()}.");
        
        // Run conversation on background thread.
        Task.Run(() => conv.RunConversationLoop(), conv.cancellationSource.Token);
    }
    
    /// <summary>
    /// Main conversation loop - runs on background thread.
    /// </summary>
    private void RunConversationLoop()
    {
        conversationThread = Thread.CurrentThread;
        Console.WriteLine($"[Conversation] Started on thread {conversationThread.ManagedThreadId}");
        
        try
        {
            // Execute "hail" keyword first.
            ExecuteKeyword("hail");
            
            // Main conversation loop.
            while (Active == this && !cancellationSource!.Token.IsCancellationRequested)
            {
                // Get next keyword from player (BLOCKS until input).
                Console.WriteLine("[Conversation] Waiting for player keyword...");
                string keyword = ConversationAsync.RequestReplyAsync().Result;
                
                Console.WriteLine($"[Conversation] Player said: '{keyword}'");
                
                // Check for bye.
                if (keyword == "bye")
                {
                    Console.WriteLine("[Conversation] Player said bye");
                    break;
                }
                
                // Execute the keyword.
                ExecuteKeyword(keyword);
                
                // Check if conversation was ended by Scheme.
                if (Active != this)
                {
                    Console.WriteLine("[Conversation] Ended by Scheme");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Conversation] Cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Conversation] Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            // Clean up on UI thread.
            ConversationAsync.RunOnUIThread(() =>
            {
                if (Active == this)
                {
                    session.SetCommandPrompt("");
                    session.LogMessage("*** END CONVERSATION ***");
                    Active = null;
                }
            });
            
            Console.WriteLine("[Conversation] Thread exiting");
        }
    }
    
    /// <summary>
    /// Execute the conversation closure with a keyword.
    /// Runs on background thread.
    /// </summary>
    private void ExecuteKeyword(string keyword)
    {
        Console.WriteLine($"[Conversation] ExecuteKeyword: '{keyword}'");
        
        var keywordSymbol = SymbolTable.StringToObject(keyword);
        
        if (conversationClosure is Callable callable)
        {
            try
            {
                callable.Call(keywordSymbol, npc, player);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Conversation] Error in keyword '{keyword}': {ex.Message}");
                ConversationAsync.LogToGame($"(Error: {ex.Message})");
            }
        }
    }
    
    /// <summary>
    /// Cancel the conversation.
    /// Safe to call from any thread.
    /// </summary>
    public void Cancel()
    {
        Console.WriteLine("[Conversation] Cancel requested");
        cancellationSource?.Cancel();
        
        if (Active == this)
            Active = null;
    }
    
    /// <summary>
    /// End the current conversation.
    /// Called by kern-conv-end from Scheme (on background thread).
    /// </summary>
    public static void End()
    {
        Console.WriteLine("[Conversation] End() called");
        
        var conv = Active;
        if (conv != null)
        {
            conv.Cancel();
            
            ConversationAsync.RunOnUIThread(() =>
            {
                conv.session.SetCommandPrompt("");
                conv.session.LogMessage("*** END CONVERSATION ***");
            });
        }
    }
    
    /// <summary>
    /// Check if we're currently in a conversation.
    /// </summary>
    public static bool IsActive => Active != null;
}
