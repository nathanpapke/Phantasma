using System;
using System.Text;
using Avalonia.Input;

namespace Phantasma.Models;

/// <summary>
/// Key handler for text input (conversations, naming, etc.).
/// 
/// Accumulates typed characters until Enter is pressed,
/// then invokes the callback with the completed input.
/// </summary>
public class TextInputHandler : IKeyHandler
{
    private readonly StringBuilder buffer = new();
    private readonly Action<string> onComplete;
    private readonly Action<string> onTextChanged;
    private readonly int maxLength;
    
    /// <summary>
    /// Create a text input handler.
    /// </summary>
    /// <param name="onComplete">Called when Enter is pressed with the final text</param>
    /// <param name="onTextChanged">Called when text changes (for UI updates)</param>
    /// <param name="maxLength">Maximum input length (default 16, Nazghul's MAX_KEYWORD_SZ)</param>
    public TextInputHandler(
        Action<string> onComplete, 
        Action<string>? onTextChanged = null,
        int maxLength = 16)
    {
        this.onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        this.onTextChanged = onTextChanged ?? (_ => { });
        this.maxLength = maxLength;
    }
    
    /// <summary>
    /// Current text in the buffer.
    /// </summary>
    public string CurrentText => buffer.ToString();
    
    /// <summary>
    /// Handle a key press.
    /// </summary>
    public bool HandleKey(Key key, string? keySymbol)
    {
        switch (key)
        {
            case Key.Enter:
                // Complete - return true to pop handler.
                onComplete(buffer.ToString());
                return true;
                
            case Key.Escape:
                // Cancel - return empty string.
                buffer.Clear();
                onComplete("");
                return true;
                
            case Key.Back:
                // Backspace.
                if (buffer.Length > 0)
                {
                    buffer.Remove(buffer.Length - 1, 1);
                    onTextChanged(buffer.ToString());
                }
                return false;
                
            default:
                // Try to add character.
                if (!string.IsNullOrEmpty(keySymbol) && keySymbol.Length == 1)
                {
                    char c = keySymbol[0];
                    
                    // Only accept printable characters.
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '\'' || c == '-')
                    {
                        if (buffer.Length < maxLength)
                        {
                            buffer.Append(c);
                            onTextChanged(buffer.ToString());
                        }
                    }
                }
                return false;
        }
    }
}