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
                dx = -1; dy = -1;
                break;
            case Key.NumPad9:
                dx = 1; dy = -1;
                break;
            case Key.NumPad1:
                dx = -1; dy = 1;
                break;
            case Key.NumPad3:
                dx = 1; dy = 1;
                break;
            default:
                return false;  // Unhandled key
        }
        
        if (dx != 0 || dy != 0)
        {
            // Update targeting coordinates in Session.
            session.MoveTarget(dx, dy);
            return false;  // Keep handling
        }
        
        return false;  // Unhandled key
    }
    
    public (int x, int y) GetTarget() => (session.TargetX, session.TargetY);
}