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
    
    /// <summary>
    /// Create a yes/no key handler.
    /// </summary>
    /// <param name="onComplete">
    /// Called when user responds.
    /// Receives true for Yes, false for No/Cancel.
    /// </param>
    public YesNoKeyHandler(Action<bool> onComplete)
    {
        this.onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
    }
    
    /// <summary>
    /// Handle a key press.
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    /// <param name="keySymbol">The character representation</param>
    /// <returns>True if handler is done and should be popped, false to continue waiting</returns>
    public bool HandleKey(Key key, string? keySymbol)
    {
        // Check the key symbol for y/n.
        if (!string.IsNullOrEmpty(keySymbol))
        {
            char c = char.ToLower(keySymbol[0]);
            
            if (c == 'y')
            {
                onComplete(true);
                return true;  // Done - yes
            }
            
            if (c == 'n')
            {
                onComplete(false);
                return true;  // Done - no
            }
        }
        
        // Check for escape (same as no).
        if (key == Key.Escape)
        {
            onComplete(false);
            return true;  // Done - cancelled (same as no)
        }
        
        // Invalid key - keep waiting
        return false;
    }
}
