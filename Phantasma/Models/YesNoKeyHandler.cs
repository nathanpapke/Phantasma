using System;
using Avalonia.Input;

namespace Phantasma.Models;

/// <summary>
/// Key handler for Yes/No confirmation prompts.
/// 
/// Used by commands that need confirmation: Quit, Drop quantity, etc.
/// 
/// Accepts Y/y for yes, N/n or Escape for no.
/// Invokes callback with the result and signals completion.
/// </summary>
public class YesNoKeyHandler : IKeyHandler
{
    private readonly Action<bool> onComplete;
    private readonly Session session;
    
    /// <summary>
    /// Result of the prompt: true = yes, false = no, null = cancelled.
    /// </summary>
    public bool? Result { get; private set; }
    
    /// <summary>
    /// Create a yes/no handler.
    /// </summary>
    /// <param name="session">Game session for UI updates</param>
    /// <param name="onComplete">Called when user responds (true=yes, false=no, null=cancel)</param>
    public YesNoKeyHandler(Session session, Action<bool> onComplete)
    {
        this.session = session;
        this.onComplete = onComplete;
    }
    
    public bool HandleKey(Key key, string? keySymbol)
    {
        switch (key)
        {
            case Key.Y:
                Result = true;
                session.UpdateCommandInput("Yes");
                onComplete?.Invoke(true);
                return true;  // Done
                
            case Key.N:
                Result = false;
                session.UpdateCommandInput("No");
                onComplete?.Invoke(false);
                return true;  // Done
                
            case Key.Escape:
                Result = null;
                session.UpdateCommandInput("(cancelled)");
                //onComplete?.Invoke(null);
                return true;  // Done
                
            default:
                // Ignore other keys.
                return false;
        }
    }
}
