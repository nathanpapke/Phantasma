using System;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

using Phantasma.Models;

namespace Phantasma.Binders;

/// <summary>
/// ViewModel for the command window.
/// Handles text display and cursor state.
/// </summary>
public partial class CommandWindowBinder : BinderBase
{
    private StringBuilder buffer;
    private int maxLength;
    private string mark;  // For marking position to erase back to
    
    [ObservableProperty]
    private string text = "";
    
    [ObservableProperty]
    private bool showCursor = false;
    
    public int CursorPosition { get; private set; }
    
    // ===================================================================
    // DIMENSION PROPERTIES - Expose layout constants for View
    // Views should NEVER reference Dimensions directly
    // ===================================================================
    
    public int AsciiHeight => Dimensions.ASCII_H;
    public int BorderWidth => Dimensions.BORDER_W;
    public int CmdHeight => Dimensions.ASCII_H + 8;  // Single line with padding
    
    public CommandWindowBinder(int maxLength = 80)
    {
        maxLength = Dimensions.STAT_CHARS_PER_LINE * 2;  // ~92 chars
        buffer = new StringBuilder(maxLength);
        mark = "";
        CursorPosition = 0;
    }
    
    /// <summary>
    /// Initialize by subscribing to Session events.
    /// </summary>
    public void Initialize(Session session)
    {
        if (session != null)
        {
            session.MessageDisplayed += OnMessageDisplayed;
            session.PromptChanged += OnPromptChanged;
        }
    }
    
    private void OnMessageDisplayed(string message)
    {
        Print(message);
    }
    
    private void OnPromptChanged(string prompt)
    {
        Clear();
        Print(prompt);
        ShowCursor = true;  // Show cursor when waiting for input.
    }
    
    /// <summary>
    /// Print text to command window (formatted).
    /// </summary>
    public void Print(string format, params object[] args)
    {
        var text = args.Length > 0 ? string.Format(format, args) : format;
        
        // Check if we have room.
        if (buffer.Length + text.Length > maxLength)
        {
            // Truncate to fit.
            text = text.Substring(0, maxLength - buffer.Length);
        }
        
        buffer.Append(text);
        CursorPosition = buffer.Length;
        Text = buffer.ToString();
    }
    
    /// <summary>
    /// Clear the command window.
    /// </summary>
    public void Clear()
    {
        buffer.Clear();
        CursorPosition = 0;
        mark = "";
        Text = "";
        ShowCursor = false;
    }
    
    /// <summary>
    /// Backspace N characters.
    /// </summary>
    public void Backspace(int n)
    {
        if (n <= 0) return;
        
        int toRemove = Math.Min(n, buffer.Length);
        if (toRemove > 0)
        {
            buffer.Remove(buffer.Length - toRemove, toRemove);
            CursorPosition = buffer.Length;
            Text = buffer.ToString();
        }
    }
    
    /// <summary>
    /// Mark current position for later erase-back-to-mark.
    /// </summary>
    public void Mark()
    {
        mark = buffer.ToString();
    }
    
    /// <summary>
    /// Erase back to marked position.
    /// </summary>
    public void EraseBackToMark()
    {
        if (string.IsNullOrEmpty(mark))
            return;
            
        int currentLength = buffer.Length;
        int markLength = mark.Length;
        
        if (currentLength > markLength)
        {
            Backspace(currentLength - markLength);
        }
    }
    
    /// <summary>
    /// Append a single character (for user input).
    /// </summary>
    public void AppendChar(char c)
    {
        if (buffer.Length < maxLength)
        {
            buffer.Append(c);
            CursorPosition = buffer.Length;
            Text = buffer.ToString();
        }
    }
    
    /// <summary>
    /// Get the current text and clear.
    /// </summary>
    public string GetAndClear()
    {
        var text = buffer.ToString();
        Clear();
        return text;
    }
}
