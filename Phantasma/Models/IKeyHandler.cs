using Avalonia.Input;

namespace Phantasma.Models;

/// <summary>
/// Interface for key input handlers.
/// 
/// Handlers are pushed onto a stack. The top handler receives all key events.
/// When a handler returns true from HandleKey(), it signals completion and
/// should be popped from the stack.
/// </summary>
public interface IKeyHandler
{
    /// <summary>
    /// Handle a key press.
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    /// <param name="keySymbol">The character representation (for text input)</param>
    /// <returns>True if handler is done and should be popped, false to continue</returns>
    bool HandleKey(Key key, string? keySymbol);
}
