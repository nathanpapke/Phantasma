using System;

namespace Phantasma.Models;

/// <summary>
/// FieldType defines a type of magical field (fire, poison, energy, etc.).
/// Fields are temporary terrain effects that affect beings who step on them.
/// 
/// Parameters from kern-mk-field-type:
/// - tag: Symbol identifier (e.g., 'F_fire)
/// - name: Display name
/// - sprite: Visual representation
/// - light: Light emitted by the field (0 = none)
/// - duration: How many turns the field lasts (-1 = permanent)
/// - pclass: Passability class (for movement costs)
/// - effect: Scheme closure called when a being steps on the field
/// </summary>
public class FieldType : ObjectType
{
    /// <summary>
    /// Light level emitted by this field type.
    /// 0 = no light, higher values illuminate the area.
    /// </summary>
    public int Light { get; set; }
    
    /// <summary>
    /// Default duration for fields of this type in turns.
    /// -1 = permanent (never expires).
    /// </summary>
    public int Duration { get; set; }
    
    /// <summary>
    /// Passability class determining movement cost through this field.
    /// </summary>
    public int PClass { get; set; }
    
    /// <summary>
    /// Scheme closure called when a being steps on this field.
    /// Can deal damage, apply effects, block movement, etc.
    /// </summary>
    public object? Effect { get; set; }
    
    public FieldType() : base()
    {
        Layer = ObjectLayer.Field;
        Light = 0;
        Duration = -1; // Permanent by default
        PClass = 0;
    }
    
    /// <summary>
    /// Full constructor matching Nazghul's FieldType signature.
    /// </summary>
    public FieldType(string tag, string name, Sprite? sprite, int light, 
                     int duration, int pclass, object? effect = null)
        : base(tag, name, ObjectLayer.Field)
    {
        Sprite = sprite;
        Light = light;
        Duration = duration;
        PClass = pclass;
        Effect = effect;
    }
    
    /// <summary>
    /// Check if fields of this type are permanent (never expire).
    /// </summary>
    public bool IsPermanent => Duration < 0;
    
    /// <summary>
    /// Create a new Field instance of this type.
    /// </summary>
    public virtual Field CreateInstance()
    {
        return new Field(this);
    }
    
    /// <summary>
    /// Create a new Field instance with custom duration.
    /// </summary>
    public virtual Field CreateInstance(int customDuration)
    {
        return new Field(this, customDuration);
    }
    
    public override string ToString()
    {
        string durStr = IsPermanent ? "permanent" : $"{Duration} turns";
        return $"FieldType({Tag}: {Name}, light={Light}, {durStr})";
    }
}
