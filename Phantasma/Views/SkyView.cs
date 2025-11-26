using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Phantasma.Views;

public class SkyView : Control
{
    public SkyView()
    {
        // Height matches Nazghul's sky bar (border height).
        // Will be set by Grid row definition.
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        double width = Bounds.Width;
        double height = Bounds.Height;
        
        // Draw dark blue gradient background (night sky placeholder).
        var skyBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(10, 10, 40), 0),    // Dark blue at top
                new GradientStop(Color.FromRgb(30, 30, 80), 1)     // Slightly lighter at bottom
            }
        };
        context.FillRectangle(skyBrush, new Rect(0, 0, width, height));
        
        // Draw border.
        var borderPen = new Pen(Brushes.DarkSlateGray, 1);
        context.DrawRectangle(borderPen, new Rect(0, 0, width, height));
        
        // Draw placeholder text.
        var typeface = new Typeface("Courier New");
        var text = new FormattedText(
            "[Sky: Time, Moons, Weather]",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            20,
            Brushes.DarkGray);
        
        // Center the text.
        double x = (width - text.Width) / 2;
        double y = (height - text.Height) / 2;
        context.DrawText(text, new Point(x, y));
    }
}