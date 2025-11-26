using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using Phantasma.Binders;

namespace Phantasma.Views;

/// <summary>
/// View for the console - displays multi-line scrollable messages.
/// This is where NPC dialog, combat log, and game messages appear.
/// </summary>
public class ConsoleView : Control
{
    private ConsoleBinder binder;
    
    public ConsoleView()
    {
        binder = new ConsoleBinder();
        DataContext = binder;
    }
    
    /// <summary>
    /// Get the binder so it can be initialized externally.
    /// </summary>
    public ConsoleBinder GetBinder()
    {
        return binder;
    }
    
    /// <summary>
    /// Subscribe to binder changes for re-rendering.
    /// </summary>
    public void SubscribeToChanges()
    {
        if (binder != null)
        {
            binder.ContentChanged += () => InvalidateVisual();
            binder.PropertyChanged += (s, e) => InvalidateVisual();
        }
    }
    
    // Padding from Binder
    private int Padding => binder?.BorderWidth / 4 ?? 4;
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        double width = Bounds.Width;
        double height = Bounds.Height;
        
        // Draw background.
        context.FillRectangle(Brushes.Black, new Rect(0, 0, width, height));
        
        // Draw border.
        var borderPen = new Pen(Brushes.Gray, 1);
        context.DrawRectangle(borderPen, new Rect(0, 0, width - 1, height - 1));
        
        if (binder == null) return;
        
        // Calculate how many lines we can show.
        int lineHeight = binder.AsciiHeight;
        int maxVisibleLines = (int)((height - Padding * 2) / lineHeight);
        
        // Draw lines from bottom up (most recent at bottom).
        var typeface = new Typeface("Courier New");
        int y = (int)height - Padding - lineHeight;
        
        // Get visible lines - we want the most recent ones at the bottom.
        var lines = binder.VisibleLines;
        int startIndex = Math.Max(0, lines.Count - maxVisibleLines);
        
        for (int i = lines.Count - 1; i >= startIndex && y >= Padding; i--)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                y -= lineHeight;
                continue;
            }
            
            // Choose color based on content.
            IBrush textBrush = GetTextBrush(line);
            
            var text = new FormattedText(
                line,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                textBrush);
            
            context.DrawText(text, new Point(Padding, y));
            y -= lineHeight;
        }
    }
    
    /// <summary>
    /// Get text color based on message content.
    /// </summary>
    private IBrush GetTextBrush(string line)
    {
        // System Messages like "[again]" or "[3 times]"
        if (line.StartsWith("[") && line.EndsWith("]"))
            return Brushes.DarkGray;
        
        // NPC Speech (could be marked differently in future)
        // For now, use white for all normal text.
        return Brushes.LightGray;
    }
}
