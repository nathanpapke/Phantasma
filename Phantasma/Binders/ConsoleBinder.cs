using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Phantasma.Binders;

/// <summary>
/// Binder for the console view - manages multi-line message display.
/// This is where NPC dialog responses, game messages, and combat log appear.
/// </summary>
public class ConsoleBinder : INotifyPropertyChanged
{
    // Configuration - matches Nazghul's console dimensions
    private const int MAX_LINES = 32;           // Circular buffer size
    private const int MAX_MSG_SIZE = 128;       // Max chars per line
    
    // Circular Buffer of Lines
    private string[] lines;
    private int currentLine;         // Index of current line being written
    private int firstLine;           // Index of first visible line in buffer
    private int numLines;            // Number of lines with content
    
    // Current Line State
    private StringBuilder currentLineBuffer;
    private int roomLeft;            // Characters remaining on current line
    
    // For repeat detection (like Nazghul's "[again]" / "[3 times]")
    private string lastMessage;
    private int repeatCount;
    
    // Observable Collection for the View
    public ObservableCollection<string> VisibleLines { get; } = new();
    
    // Dimension Properties for the View (from Dimensions)
    public int AsciiWidth => 8;      // ASCII_W
    public int AsciiHeight => 16;    // ASCII_H
    public int BorderWidth => 16;    // BORDER_W
    
    // Events
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? ContentChanged;
    
    public ConsoleBinder()
    {
        lines = new string[MAX_LINES];
        for (int i = 0; i < MAX_LINES; i++)
            lines[i] = string.Empty;
        
        currentLineBuffer = new StringBuilder(MAX_MSG_SIZE);
        currentLine = 0;
        firstLine = 0;
        numLines = 1;  // Always at least one line (current)
        roomLeft = MAX_MSG_SIZE;
        lastMessage = string.Empty;
        repeatCount = 0;
        
        UpdateVisibleLines();
    }
    
    /// <summary>
    /// Print a message to the console with word wrapping.
    /// Supports printf-style format strings.
    /// </summary>
    public void Print(string format, params object[] args)
    {
        string message = args.Length > 0 ? string.Format(format, args) : format;
        
        // Check for repeated message.
        if (HandleRepeat(message))
            return;
        
        // Process each character with word wrapping.
        int i = 0;
        while (i < message.Length)
        {
            // Handle newlines.
            if (message[i] == '\n')
            {
                CommitCurrentLine();
                Newline();
                i++;
                continue;
            }
            
            // Find next word boundary.
            int wordEnd = i;
            while (wordEnd < message.Length && !char.IsWhiteSpace(message[wordEnd]))
                wordEnd++;
            
            int wordLength = wordEnd - i;
            
            // If word won't fit on current line, wrap to next line.
            if (wordLength > roomLeft && currentLineBuffer.Length > 0)
            {
                CommitCurrentLine();
                Newline();
            }
            
            // Write the word (or as much as fits).
            while (i < wordEnd && roomLeft > 0)
            {
                currentLineBuffer.Append(message[i]);
                roomLeft--;
                i++;
            }
            
            // Write trailing whitespace (except newlines).
            while (i < message.Length && char.IsWhiteSpace(message[i]) && message[i] != '\n')
            {
                if (roomLeft > 0)
                {
                    currentLineBuffer.Append(message[i]);
                    roomLeft--;
                }
                i++;
            }
            
            // If line is full, wrap.
            if (roomLeft == 0)
            {
                CommitCurrentLine();
                Newline();
            }
        }
        
        // Commit any remaining content.
        CommitCurrentLine();
        UpdateVisibleLines();
    }
    
    /// <summary>
    /// Print a message followed by a newline.
    /// </summary>
    public void PrintLine(string format, params object[] args)
    {
        Print(format, args);
        Newline();
    }
    
    /// <summary>
    /// Handle repeated messages like Nazghul does ("[again]", "[3 times]").
    /// </summary>
    private bool HandleRepeat(string message)
    {
        // Only check for repeats on complete messages (ending with newline or standalone).
        if (message == lastMessage && !string.IsNullOrEmpty(message))
        {
            repeatCount++;
            
            if (repeatCount == 1)
            {
                // First repeat - add "[again]" on new line
                if (currentLineBuffer.Length > 0)
                {
                    CommitCurrentLine();
                    Newline();
                }
                lines[currentLine] = "[again]";
            }
            else
            {
                // Subsequent repeats - update count
                lines[currentLine] = $"[{repeatCount + 1} times]";
            }
            
            UpdateVisibleLines();
            return true;
        }
        
        // Not a repeat - reset tracking
        if (repeatCount > 0)
        {
            Newline();
        }
        repeatCount = 0;
        lastMessage = message;
        return false;
    }
    
    /// <summary>
    /// Commit the current line buffer to the lines array.
    /// </summary>
    private void CommitCurrentLine()
    {
        lines[currentLine] = currentLineBuffer.ToString();
    }
    
    /// <summary>
    /// Advance to the next line.
    /// </summary>
    public void Newline()
    {
        CommitCurrentLine();
        
        // Advance to next line in circular buffer.
        currentLine = (currentLine + 1) % MAX_LINES;
        
        if (numLines < MAX_LINES)
            numLines++;
        else
            firstLine = (firstLine + 1) % MAX_LINES;
        
        // Reset current line.
        currentLineBuffer.Clear();
        lines[currentLine] = string.Empty;
        roomLeft = MAX_MSG_SIZE;
        
        UpdateVisibleLines();
    }
    
    /// <summary>
    /// Backspace: remove characters from current line.
    /// </summary>
    public void Backspace(int count)
    {
        int toRemove = Math.Min(count, currentLineBuffer.Length);
        if (toRemove > 0)
        {
            currentLineBuffer.Length -= toRemove;
            roomLeft += toRemove;
            CommitCurrentLine();
            UpdateVisibleLines();
        }
    }
    
    /// <summary>
    /// Clear all console content.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < MAX_LINES; i++)
            lines[i] = string.Empty;
        
        currentLineBuffer.Clear();
        currentLine = 0;
        firstLine = 0;
        numLines = 1;
        roomLeft = MAX_MSG_SIZE;
        repeatCount = 0;
        lastMessage = string.Empty;
        
        UpdateVisibleLines();
    }
    
    /// <summary>
    /// Update the observable collection for the view.
    /// Shows lines from firstLine to currentLine in order.
    /// </summary>
    private void UpdateVisibleLines()
    {
        VisibleLines.Clear();
        
        int line = firstLine;
        for (int i = 0; i < numLines; i++)
        {
            VisibleLines.Add(lines[line]);
            line = (line + 1) % MAX_LINES;
        }
        
        OnPropertyChanged(nameof(VisibleLines));
        ContentChanged?.Invoke();
    }
    
    /// <summary>
    /// Get the number of lines that can fit in a given height.
    /// </summary>
    public int GetVisibleLineCount(double height)
    {
        return Math.Max(1, (int)(height / AsciiHeight));
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
