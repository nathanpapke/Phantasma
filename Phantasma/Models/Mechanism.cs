using System;
using IronScheme.Runtime;
using IronScheme.Scripting;

namespace Phantasma.Models;

/// <summary>
/// Mechanisms (levers, buttons, switches)
/// </summary>
public class Mechanism : Object, IBlockingObject
{
    public override ObjectLayer Layer => ObjectLayer.Mechanism;
    
    // Mechanism-specific Properties
    public bool IsActivated { get; set; }
    public bool IsPassable { get; set; } = false;
    
    public Mechanism()
    {
        PassabilityClass = 8;
    }
    
    /// <summary>
    /// Handle this mechanism (open door, pull lever, etc.).
    /// Calls the Scheme closure: (gifc 'handle this handler).
    /// </summary>
    public override void Handle(Character handler)
    {
        var closure = Type?.InteractionHandler;
        if (closure == null)
        {
            Console.WriteLine($"{Name}: no interaction handler defined");
            return;
        }
        
        try
        {
            if (closure is Callable callable)
            {
                var handleSymbol = SymbolTable.StringToObject("handle");
                callable.Call(handleSymbol, this, handler);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Handle error on {Name}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Synchronize mechanism state with game time.
    /// Sends 'sync signal to the interaction handler if one exists.
    /// </summary>
    public override void Synchronize()
    {
        base.Synchronize();
    
        // Send 'sync signal to allow Scheme handler to update state if needed.
        // This is similar to 'init but specifically for time-based sync.
        var gifc = Type?.InteractionHandler;
        if (gifc == null) 
            return;
    
        try
        {
            if (gifc is Callable callable)
            {
                var syncSymbol = SymbolTable.StringToObject("sync");
                callable.Call(syncSymbol, this);
            }
        }
        catch
        {
            // 'sync handler may not exist for this mechanism - that's fine.
        }
    }
}
