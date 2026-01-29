using System;
using Avalonia.Input;

namespace Phantasma.Models;

public class TargetingKeyHandler : IKeyHandler
{
    private readonly Session session;
    
    public bool Aborted { get; private set; }
    public Action<bool, int, int> OnComplete { get; set; }
    
    public TargetingKeyHandler(Session session, int originX, int originY, 
                               int range, int startX, int startY)
    {
        this.session = session;
        // Note: Session.BeginTargeting already set up the targeting state.
        // We just need to respond to input
    }
    
    public bool HandleKey(Key key, string? keySymbol)
    {
        // Enter/Space = Select target
        if (key == Key.Enter || key == Key.Space)
        {
            OnComplete?.Invoke(true, session.TargetX, session.TargetY);
            return true;  // Done
        }
        
        // Escape = Cancel
        if (key == Key.Escape)
        {
            Aborted = true;
            OnComplete?.Invoke(false, session.TargetX, session.TargetY);
            return true;  // Done
        }
        
        // Arrow keys = Move target cursor
        int dx = 0, dy = 0;
        
        switch (key)
        {
            case Key.Up:
            case Key.NumPad8:
                dy = -1;
                break;
            case Key.Down:
            case Key.NumPad2:
                dy = 1;
                break;
            case Key.Left:
            case Key.NumPad4:
                dx = -1;
                break;
            case Key.Right:
            case Key.NumPad6:
                dx = 1;
                break;
            case Key.NumPad7:
            case Key.Home:
                dx = -1; dy = -1;
                break;
            case Key.NumPad9:
            case Key.PageUp:
                dx = 1; dy = -1;
                break;
            case Key.NumPad1:
            case Key.End:
                dx = -1; dy = 1;
                break;
            case Key.NumPad3:
            case Key.PageDown:
                dx = 1; dy = 1;
                break;
            case Key.NumPad5:
                // "Here" - select current position (only for range 1)
                if (session.TargetRange == 1)
                {
                    OnComplete?.Invoke(true, session.TargetX, session.TargetY);
                    return true;
                }
                return false;
            default:
                return false;  // Unhandled key
        }
        
        if (dx != 0 || dy != 0)
        {
            bool moved = session.MoveTarget(dx, dy);
            
            // Range 1: auto-confirm on successful move.
            if (moved && session.TargetRange == 1)
            {
                OnComplete?.Invoke(true, session.TargetX, session.TargetY);
                return true;  // Done
            }
            
            return false;  // Keep handling
        }
        
        return false;  // Unhandled key
    }
    
    public (int x, int y) GetTarget() => (session.TargetX, session.TargetY);
}