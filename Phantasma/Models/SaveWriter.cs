using System;
using System.IO;
using System.Text;

namespace Phantasma.Models;

/// <summary>
/// Writer for Creating Scheme-formatted Save Files
/// </summary>
public class SaveWriter : IDisposable
{
    private readonly StreamWriter writer;
    private int indent = 0;
    private const int IndentWidth = 2;
    
    public SaveWriter(string filename)
    {
        writer = new StreamWriter(filename, false, Encoding.UTF8);
    }
    
    /// <summary>
    /// Write a line with current indentation.
    /// </summary>
    public void WriteLine(string text)
    {
        if (indent > 0)
        {
            writer.Write(new string(' ', indent));
        }
        writer.WriteLine(text);
    }
    
    /// <summary>
    /// Write formatted text with current indentation.
    /// </summary>
    public void WriteLine(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }
    
    /// <summary>
    /// Write text with current indentation, no newline.
    /// </summary>
    public void Write(string text)
    {
        if (indent > 0)
        {
            writer.Write(new string(' ', indent));
        }
        writer.Write(text);
    }
    
    /// <summary>
    /// Write text without indentation (append to current line).
    /// </summary>
    public void Append(string text)
    {
        writer.Write(text);
    }
    
    /// <summary>
    /// Write text with indentation and increase indent level.
    /// </summary>
    public void Enter(string text)
    {
        WriteLine(text);
        indent += IndentWidth;
    }
    
    /// <summary>
    /// Write formatted text with indentation and increase indent level.
    /// </summary>
    public void Enter(string format, params object[] args)
    {
        Enter(string.Format(format, args));
    }
    
    /// <summary>
    /// Decrease indent level and write text.
    /// </summary>
    public void Exit(string text)
    {
        indent -= IndentWidth;
        if (indent < 0)
        {
            indent = 0;
        }
        WriteLine(text);
    }
    
    /// <summary>
    /// Decrease indent level and write formatted text.
    /// </summary>
    public void Exit(string format, params object[] args)
    {
        Exit(string.Format(format, args));
    }
    
    /// <summary>
    /// Write a comment line.
    /// </summary>
    public void WriteComment(string comment)
    {
        WriteLine($";; {comment}");
    }
    
    public void Dispose()
    {
        if (indent != 0)
        {
            Console.Error.WriteLine($"Warning: SaveWriter disposed with indent = {indent}");
        }
        writer.Flush();
        writer.Dispose();
    }
}
