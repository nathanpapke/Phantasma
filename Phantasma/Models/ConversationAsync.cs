using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

/// <summary>
/// Async/Task-based conversation input system.
/// 
/// This design supports BOTH usage patterns with the same API:
/// 
/// 1. BLOCKING (Phantasma - background thread):
///    bool answer = ConversationAsync.RequestYesNoAsync().Result;
///    
/// 2. ASYNC (Future projects):
///    bool answer = await ConversationAsync.RequestYesNoAsync();
/// 
/// The conversation runs on a background thread (for blocking) or uses
/// async/await (for real-time). UI updates are always marshaled to the
/// UI thread via Dispatcher.
/// </summary>
public class ConversationAsync
{
    // ===================================================================
    // CONFIGURATION
    // ===================================================================
    
    /// <summary>
    /// When true, adds extra logging for debugging.
    /// </summary>
    public static bool DebugLogging { get; set; } = true;
    
    // ===================================================================
    // YES/NO INPUT
    // ===================================================================
    
    /// <summary>
    /// Request yes/no input from the user.
    /// 
    /// Usage (blocking):  bool answer = RequestYesNoAsync().Result;
    /// Usage (async):     bool answer = await RequestYesNoAsync();
    /// </summary>
    public static Task<bool> RequestYesNoAsync()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Must set up UI on the UI thread.
        RunOnUIThread(() =>
        {
            var session = Phantasma.MainSession;
            if (session == null)
            {
                Log("[RequestYesNoAsync] No session - returning false");
                tcs.TrySetResult(false);
                return;
            }
            
            Log("[RequestYesNoAsync] Pushing YesNoKeyHandler...");
            session.SetCommandPrompt("<Y/N>? ");
            
            var handler = new YesNoKeyHandler(session, result =>
            {
                session.SetCommandPrompt("");
                bool answer = result;
                
                Log($"[RequestYesNoAsync] Got answer: {(answer ? "Yes" : "No")}");
                session.LogMessage(answer ? "Yes" : "No");
                
                // Complete the task - unblocks the waiting thread
                tcs.TrySetResult(answer);
            });
            
            session.PushKeyHandler(handler);
        });
        
        return tcs.Task;
    }
    
    // ===================================================================
    // AMOUNT INPUT
    // ===================================================================
    
    /// <summary>
    /// Request numeric amount input from the user.
    /// 
    /// Usage (blocking):  int amount = RequestAmountAsync().Result;
    /// Usage (async):     int amount = await RequestAmountAsync();
    /// </summary>
    public static Task<int> RequestAmountAsync(int maxValue = -1)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        RunOnUIThread(() =>
        {
            var session = Phantasma.MainSession;
            if (session == null)
            {
                Log("[RequestAmountAsync] No session - returning 0");
                tcs.TrySetResult(0);
                return;
            }
            
            Log("[RequestAmountAsync] Pushing AmountKeyHandler...");
            session.SetCommandPrompt("How many? ");
            
            var handler = new AmountKeyHandler(session, result =>
            {
                session.SetCommandPrompt("");
                int amount = result ?? 0;
                
                Log($"[RequestAmountAsync] Got amount: {amount}");
                session.LogMessage(amount.ToString());
                
                tcs.TrySetResult(amount);
            }, maxValue);
            
            session.PushKeyHandler(handler);
        });
        
        return tcs.Task;
    }
    
    // ===================================================================
    // TEXT/REPLY INPUT
    // ===================================================================
    
    /// <summary>
    /// Request text reply input from the user.
    /// Returns the keyword truncated to 4 characters (Nazghul behavior).
    /// 
    /// Usage (blocking):  string reply = RequestReplyAsync().Result;
    /// Usage (async):     string reply = await RequestReplyAsync();
    /// </summary>
    public static Task<string> RequestReplyAsync()
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        RunOnUIThread(() =>
        {
            var session = Phantasma.MainSession;
            if (session == null)
            {
                Log("[RequestReplyAsync] No session - returning 'bye'");
                tcs.TrySetResult("bye");
                return;
            }
            
            Log("[RequestReplyAsync] Pushing TextInputHandler...");
            session.SetCommandPrompt("Say: ");
            
            var handler = new TextInputHandler(
                onComplete: text =>
                {
                    session.SetCommandPrompt("");
                    
                    string keyword = string.IsNullOrWhiteSpace(text) ? "bye" : text.ToLower().Trim();
                    if (keyword.Length > 4)
                        keyword = keyword.Substring(0, 4);
                    
                    Log($"[RequestReplyAsync] Got reply: '{keyword}'");
                    
                    string playerName = session.Player?.GetName() ?? "You";
                    session.LogMessage($"{playerName}: {keyword}");
                    
                    tcs.TrySetResult(keyword);
                },
                onTextChanged: text => session.UpdateCommandInput(text)
            );
            
            session.PushKeyHandler(handler);
        });
        
        return tcs.Task;
    }
    
    // ===================================================================
    // UI OUTPUT (Thread-Safe)
    // ===================================================================
    
    /// <summary>
    /// Display a message in the conversation log.
    /// Safe to call from any thread.
    /// </summary>
    public static void Say(string speaker, string message)
    {
        string fullMessage = $"{speaker}: {message}";
        Log($"[Say] {fullMessage}");
        
        RunOnUIThread(() =>
        {
            Phantasma.MainSession?.LogMessage(fullMessage);
        });
    }
    
    /// <summary>
    /// Display a message in the conversation log (no speaker prefix).
    /// Safe to call from any thread.
    /// </summary>
    public static void Log(string message)
    {
        if (DebugLogging)
        {
            Console.WriteLine($"[ConversationAsync] {message}");
        }
    }
    
    /// <summary>
    /// Log a message to the game's message log.
    /// Safe to call from any thread.
    /// </summary>
    public static void LogToGame(string message)
    {
        RunOnUIThread(() =>
        {
            Phantasma.MainSession?.LogMessage(message);
        });
    }
    
    // ===================================================================
    // THREAD UTILITIES
    // ===================================================================
    
    /// <summary>
    /// Run an action on the UI thread.
    /// If already on UI thread, runs immediately.
    /// </summary>
    public static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            // Already on UI thread
            action();
        }
        else
        {
            // Marshal to UI thread
            Dispatcher.UIThread.Post(action);
        }
    }
    
    /// <summary>
    /// Run an action on the UI thread and wait for completion.
    /// Use sparingly - can cause deadlocks if misused.
    /// </summary>
    public static void RunOnUIThreadAndWait(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(action).Wait();
        }
    }
    
    /// <summary>
    /// Run a function on the UI thread and return the result.
    /// </summary>
    public static T RunOnUIThread<T>(Func<T> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return func();
        }
        else
        {
            return Dispatcher.UIThread.InvokeAsync(func).Result;
        }
    }
}
