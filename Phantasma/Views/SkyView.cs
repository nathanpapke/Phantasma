using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using Phantasma.Binders;

namespace Phantasma.Views;

/// <summary>
/// Renders the sky bar showing astral bodies (sun, moons), time, and wind.
/// </summary>
public class SkyView : Control
{
    // Sprite Size for Astral Bodies (half tile width)
    private const int SpriteSize = 16;
    
    private SkyBinder _binder;
    
    public SkyView()
    {
        _binder = new SkyBinder();
        DataContext = _binder;
    }
    
    /// <summary>
    /// Get the binder so it can be initialized externally.
    /// </summary>
    public SkyBinder GetBinder()
    {
        return _binder;
    }
    
    /// <summary>
    /// Subscribe to binder changes for re-rendering.
    /// </summary>
    public void SubscribeToChanges()
    {
        if (_binder != null)
        {
            _binder.PropertyChanged += OnBinderPropertyChanged;
        }
    }
    
    /// <summary>
    /// Bind to a SkyBinder for data.
    /// </summary>
    public void Bind(SkyBinder binder)
    {
        if (_binder != null)
        {
            _binder.PropertyChanged -= OnBinderPropertyChanged;
        }
        
        _binder = binder;
        
        if (_binder != null)
        {
            _binder.PropertyChanged += OnBinderPropertyChanged;
        }
        
        InvalidateVisual();
    }
    
    private void OnBinderPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }
    
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _binder?.SetViewSize(e.NewSize.Width, e.NewSize.Height);
    }

    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        double width = Bounds.Width;
        double height = Bounds.Height;
        
        // Draw sky background (color based on time of day).
        DrawSkyBackground(context, width, height);
        
        // Draw astral bodies (sun, moons).
        DrawAstralBodies(context);
        
        // Draw time display on left.
        DrawTimeDisplay(context, height);
        
        // Draw wind display on right.
        DrawWindDisplay(context, width, height);
        
        // Draw border.
        var borderPen = new Pen(Brushes.DarkSlateGray, 1);
        context.DrawRectangle(borderPen, new Rect(0, 0, width, height));
    }
    
    private void DrawSkyBackground(DrawingContext context, double width, double height)
    {
        byte r1 = _binder?.SkyTopR ?? 10;
        byte g1 = _binder?.SkyTopG ?? 10;
        byte b1 = _binder?.SkyTopB ?? 40;
        byte r2 = _binder?.SkyBottomR ?? 30;
        byte g2 = _binder?.SkyBottomG ?? 30;
        byte b2 = _binder?.SkyBottomB ?? 80;
        
        var skyBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(r1, g1, b1), 0),
                new GradientStop(Color.FromRgb(r2, g2, b2), 1)
            }
        };
        
        context.FillRectangle(skyBrush, new Rect(0, 0, width, height));
    }
    
    private void DrawAstralBodies(DrawingContext context)
    {
        if (_binder == null)
            return;
    
        int spriteSize = _binder.AstralBodySpriteSize;
    
        foreach (var body in _binder.AstralBodies)
        {
            if (body.HasSprite && body.Image != null)
            {
                var destRect = new Rect(body.X, body.Y, spriteSize, spriteSize);
                context.DrawImage(body.Image, body.SourceRect, destRect);
            }
            else
            {
                // Fallback Circle
                var brush = new SolidColorBrush(Color.FromRgb(
                    body.FallbackColorR,
                    body.FallbackColorG,
                    body.FallbackColorB));
                var center = new Point(body.X + spriteSize / 2, body.Y + spriteSize / 2);
                context.DrawEllipse(brush, null, center, spriteSize / 2 - 1, spriteSize / 2 - 1);
            }
        }
    }
    
    private void DrawTimeDisplay(DrawingContext context, double height)
    {
        string timeStr = _binder?.TimeString ?? "12:00PM";
        
        var typeface = new Typeface("Courier New", FontStyle.Normal, FontWeight.Bold);
        
        // Shadow
        var shadowText = new FormattedText(timeStr,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, 12, Brushes.Black);
        
        double y = (height - shadowText.Height) / 2;
        context.DrawText(shadowText, new Point(5, y + 1));
        
        // Text
        var text = new FormattedText(timeStr,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, 12, Brushes.White);
        context.DrawText(text, new Point(4, y));
    }
    
    private void DrawWindDisplay(DrawingContext context, double width, double height)
    {
        string display = $"Wind:{_binder?.WindString ?? "North"}";
        
        var typeface = new Typeface("Courier New", FontStyle.Normal, FontWeight.Bold);
        var text = new FormattedText(display,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, 12, Brushes.LightGray);
        
        double x = width - text.Width - 4;
        double y = (height - text.Height) / 2;
        context.DrawText(text, new Point(x, y));
    }
}
