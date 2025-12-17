namespace Phantasma.Models;

/// <summary>
/// An instance of an effect attached to an object.
/// Stored in Object's hooks array.
/// </summary>
public struct Hook
{
    /// <summary>The effect type.</summary>
    public Effect Effect;
    
    /// <summary>Per-instance Scheme data (gob).</summary>
    public object? Gob;
    
    /// <summary>Expiration time (clock ticks, or -1 for never).</summary>
    public int Expiration;
    
    /// <summary>Effect flags (for save/load).</summary>
    public int Flags;
    
    public Hook(Effect effect, object? gob = null, int expiration = -1, int flags = 0)
    {
        Effect = effect;
        Gob = gob;
        Expiration = expiration;
        Flags = flags;
    }
    
    /// <summary>Check if this hook has expired.</summary>
    public bool HasExpired(int currentTime) => Expiration >= 0 && currentTime >= Expiration;
}
