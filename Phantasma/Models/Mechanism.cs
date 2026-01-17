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
}
