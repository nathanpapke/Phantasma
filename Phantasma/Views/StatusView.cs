using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using Phantasma.Binders;

namespace Phantasma.Views;

/// <summary>
/// View for the status window.
/// Renders party info, character stats, inventory, etc.
/// </summary>
public class StatusView : Control
{
    private StatusBinder binder;
    
    public StatusView()
    {
        binder = new StatusBinder();
        DataContext = binder;
    }
    
    /// <summary>
    /// Report preferred size based on content.
    /// Status window height depends on number of party members.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Base Height: title + padding
        double height = 30;
        
        if (binder != null && binder.IsShowingParty)
        {
            // Each party member takes ~2 lines (name + HP/status).
            int charHeight = binder.AsciiHeight;
            height += binder.PartyMembers.Count * (charHeight * 2 + 4);
            height += 10;  // Bottom padding
        }
        else
        {
            // Default Height for Other Modes
            height = 150;
        }
        
        return new Size(availableSize.Width, Math.Min(height, availableSize.Height));
    }
    
    /// <summary>
    /// Get the binder so it can be initialized externally.
    /// </summary>
    public StatusBinder GetBinder()
    {
        return binder;
    }
    
    /// <summary>
    /// Called after binder is initialized to set up change notifications
    /// and finalize layout dimensions.
    /// </summary>
    public void SubscribeToChanges()
    {
        if (binder != null)
        {
            if (binder != null)
            {
                binder.DisplayChanged += () => InvalidateVisual();
                binder.PropertyChanged += (s, e) => InvalidateVisual();
            }
        }
    }
    
    // Helper to get Padding from Binder
    private int Padding => binder?.BorderWidth / 4 ?? 4;
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Draw background.
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        // Draw border.
        var borderPen = new Pen(Brushes.Gray, 2);
        context.DrawRectangle(borderPen, new Rect(1, 1, Bounds.Width - 2, Bounds.Height - 2));
        
        if (binder == null) return;
        
        // Draw title.
        DrawTitle(context, binder.Title, Bounds.Width);
        
        // Draw content based on binder state.
        if (binder.IsShowingParty)
        {
            DrawPartyList(context, Bounds.Width, Bounds.Height);
        }
        else if (binder.IsShowingStats)
        {
            DrawCharacterStats(context, Bounds.Width, Bounds.Height);
        }
        else if (binder.IsShowingPage)
        {
            DrawPageText(context, Bounds.Width, Bounds.Height);
        }
        else
        {
            DrawPlaceholder(context, "Other Mode");
        }
    }
    
    private void DrawTitle(DrawingContext context, string title, double width)
    {
        var typeface = new Typeface("Courier New");
        var text = new FormattedText(
            title,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            Brushes.White);
        
        // Center title.
        double x = (width - text.Width) / 2;
        context.DrawText(text, new Point(x, Padding));
    }
    
    private void DrawPartyList(DrawingContext context, double width, double height)
    {
        if (binder.PartyMembers.Count == 0) return;
        
        var typeface = new Typeface("Courier New");
        int charHeight = binder.AsciiHeight;
        int y = Padding + 20;
        
        foreach (var member in binder.PartyMembers)
        {
            // Draw name.
            var nameText = new FormattedText(
                member.Name,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                member.IsSelected ? Brushes.Yellow : Brushes.White);
            
            context.DrawText(nameText, new Point(Padding + 4, y));
            y += charHeight;
            
            // Draw HP and condition.
            var hpText = new FormattedText(
                $"{member.HP}/{member.MaxHP} {member.Condition}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                Brushes.LightGray);
            
            context.DrawText(hpText, new Point(width - hpText.Width - hpText.Width - Padding - 4, y));
            y += charHeight + 4;
            
            // Shade non-selected members in select mode.
            if (!member.IsSelected && binder.IsSelectMode)
            {
                var shadeRect = new Rect(Padding, y - charHeight * 2 - 4, 
                                        Width - Padding * 2, charHeight * 2 + 4);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)), shadeRect);
            }
        }
    }
    
    private void DrawCharacterStats(DrawingContext context, double width, double height)
    {
        if (binder.StatLines.Count == 0) return;
        
        var typeface = new Typeface("Courier New");
        int charHeight = binder.AsciiHeight;
        int y = Padding + 20;
        
        foreach (var line in binder.StatLines)
        {
            // Draw label.
            var labelText = new FormattedText(
                line.Label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                Brushes.White);
            
            context.DrawText(labelText, new Point(Padding + 4, y));
            
            // Draw value (right-aligned if present).
            if (!string.IsNullOrEmpty(line.Value))
            {
                var valueText = new FormattedText(
                    line.Value,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    Brushes.LightGray);
                
                context.DrawText(valueText, 
                    new Point(width - valueText.Width - valueText.Width - Padding - 4, y));
            }
            
            y += charHeight;
        }
    }
    
    private void DrawPageText(DrawingContext context, double width, double height)
    {
        if (string.IsNullOrEmpty(binder.PageText)) return;
        
        var typeface = new Typeface("Courier New");
        int charHeight = binder.AsciiHeight;
        int y = Padding + 20 - binder.PageScrollY;
        
        // Split into lines and render.
        var lines = binder.PageText.Split('\n');
        
        foreach (var line in lines)
        {
            if (y > -charHeight && y < Height)  // Only render visible lines.
            {
                var text = new FormattedText(
                    line,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    Brushes.White);
                
                context.DrawText(text, new Point(Padding + 4, y));
            }
            
            y += charHeight;
        }
    }
    
    private void DrawPlaceholder(DrawingContext context, string mode)
    {
        var typeface = new Typeface("Courier New");
        var text = new FormattedText(
            $"[{mode}]",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            12,
            Brushes.Gray);
        
        context.DrawText(text, new Point(Padding + 4, Padding + 20));
    }
}
