namespace Phantasma.Models;

/// <summary>
/// Fields (fire, poison, energy)
/// </summary>
public class Field : Object
{
    public override ObjectLayer Layer => ObjectLayer.Field;
    
    /// <summary>
    /// Remaining duration in turns. Decremented each turn.
    /// -1 means permanent (never expires).
    /// </summary>
    public int Duration { get; set; } = -1;
    
    /// <summary>
    /// Flag indicating this field has been destroyed/expired.
    /// </summary>
    private bool destroyed = false;
    
    public Field() : base()
    {
        Duration = -1;
    }
    
    /// <summary>
    /// Create a field from a FieldType, using its default duration.
    /// </summary>
    public Field(FieldType type) : base()
    {
        Type = type;
        Duration = type.Duration;
    }
    
    /// <summary>
    /// Create a field from a FieldType with custom duration.
    /// </summary>
    public Field(FieldType type, int duration) : base()
    {
        Type = type;
        Duration = duration;
    }
    
    /// <summary>
    /// Get the FieldType for this field.
    /// </summary>
    public FieldType? GetFieldType()
    {
        return Type as FieldType;
    }
    
    /// <summary>
    /// Get light level emitted by this field.
    /// </summary>
    public int GetLight()
    {
        return GetFieldType()?.Light ?? 0;
    }
    
    /// <summary>
    /// Get passability class for this field.
    /// </summary>
    public int GetPClass()
    {
        return GetFieldType()?.PClass ?? 0;
    }
    
    /// <summary>
    /// Check if this field is permanent (never expires).
    /// </summary>
    public bool IsPermanent => Duration < 0;
    
    /// <summary>
    /// Check if this field has been destroyed.
    /// </summary>
    public bool IsDestroyed() => destroyed;
    
    /// <summary>
    /// Called when a being steps on this field.
    /// Runs the effect closure if one exists.
    /// </summary>
    public void TriggerEffect(Being target)
    {
        var fieldType = GetFieldType();
        if (fieldType?.Effect == null)
            return;
        
        // TODO: Call the Scheme closure with the target.
        // For now, we'd need to invoke through the Kernel.
        // closure_exec(fieldType.Effect, target);
    }
    
    /// <summary>
    /// Execute the field's per-turn behavior.
    /// Decrements duration and destroys the field when it expires.
    /// </summary>
    public void Exec()
    {
        if (destroyed)
            return;
        
        // Permanent fields never expire.
        if (IsPermanent)
            return;
        
        // Decrement duration.
        Duration--;
        
        // Destroy when expired.
        if (Duration <= 0)
        {
            Destroy();
        }
    }
    
    /// <summary>
    /// Destroy this field, removing it from play.
    /// </summary>
    public void Destroy()
    {
        destroyed = true;
        
        // Remove from place if placed.
        Position?.Place?.RemoveObject(this);
    }
    
    public override string ToString()
    {
        var fieldType = GetFieldType();
        string durStr = IsPermanent ? "permanent" : $"{Duration} turns";
        return $"Field({fieldType?.Tag ?? "unknown"}: {durStr})";
    }
}
