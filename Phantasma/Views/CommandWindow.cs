using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using Phantasma.Binders;

namespace Phantasma.Views;

/// <summary>
/// View for the command window (text input/output line at bottom).
/// 
/// IMPORTANT: This View only knows about CommandWindowBinder.
/// It NEVER references any classes from Phantasma.Models.
/// All dimension values come from the Binder.
/// </summary>
public class CommandWindow : Control
{
    private CommandWindowBinder binder;
    private DispatcherTimer cursorTimer;
    private bool cursorVisible = true;
    
    public CommandWindow()
    {
        binder = new CommandWindowBinder();
        DataContext = binder;
        
        // Subscribe to binder property changes.
        binder.PropertyChanged += (s, e) => InvalidateVisual();
        
        // Setup cursor blink timer.
        cursorTimer = new DispatcherTimer();
        cursorTimer.Interval = TimeSpan.FromMilliseconds(500);
        cursorTimer.Tick += (s, e) => 
        { 
            cursorVisible = !cursorVisible;
            InvalidateVisual();
        };
        cursorTimer.Start();
    }
    
    public CommandWindowBinder GetBinder()
    {
        return binder;
    }
    
    // Helper to get padding from binder.
    private int Padding => binder?.BorderWidth / 4 ?? 4;
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Draw background.
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        // Draw border
        var borderPen = new Pen(Brushes.Gray, 1);
        context.DrawRectangle(borderPen, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        if (string.IsNullOrEmpty(binder.Text))
            return;
        
        // Draw text.
        var typeface = new Typeface("Courier New");
        var text = new FormattedText(
            binder.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            Brushes.White);
        
        context.DrawText(text, new Point(Padding, (Bounds.Height - text.Height) / 2));
        
        // Draw cursor if waiting for input.
        if (binder.ShowCursor && cursorVisible)
        {
            // Measure single character width (monospace font)
            var charMeasure = new FormattedText(
                "X",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                14,
                Brushes.White);

            double cursorX = Padding + (binder.CursorPosition * charMeasure.Width);
            
            var cursor = new FormattedText(
                "_",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, 
                14,
                Brushes.Yellow);
            
            context.DrawText(cursor, new Point(cursorX, (Bounds.Height - cursor.Height) / 2));
        }
    }
}
