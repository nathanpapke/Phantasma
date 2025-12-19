namespace Phantasma.Models;

/// <summary>
/// Represents a celestial body (sun, moon, star).
/// </summary>
public class AstralBody
{
    public string Tag { get; set; }
    public string Name { get; set; }
    
    /// <summary>
    /// Relative distance - used for rendering order (far to near).
    /// </summary>
    public int Distance { get; set; }
    
    /// <summary>
    /// Minutes per phase change (0 = no phase changes, like the sun).
    /// </summary>
    public int MinutesPerPhase { get; set; }
    
    /// <summary>
    /// Minutes per degree of arc movement.
    /// For the sun: 4 (360 degrees / 1440 minutes per day).
    /// </summary>
    public int MinutesPerDegree { get; set; }
    
    /// <summary>
    /// Starting arc position (0-359 degrees).
    /// </summary>
    public int InitialArc { get; set; }
    
    /// <summary>
    /// Starting phase index.
    /// </summary>
    public int InitialPhase { get; set; }
    
    /// <summary>
    /// Current arc position (0-359 degrees).
    /// 0 = midnight, 90 = 6am, 180 = noon, 270 = 6pm.
    /// </summary>
    public int Arc { get; set; }
    
    /// <summary>
    /// Current phase index.
    /// </summary>
    public int PhaseIndex { get; set; }
    
    /// <summary>
    /// Current light output (0-255).
    /// </summary>
    public int Light { get; set; }
    
    /// <summary>
    /// Array of phases for this body.
    /// </summary>
    public Phase[] Phases { get; set; }
    
    /// <summary>
    /// Callback invoked when phase changes.
    /// </summary>
    public object PhaseChangeCallback { get; set; }
    
    /// <summary>
    /// Gob (game object) attached to this body.
    /// </summary>
    public Gob? Gob { get; set; }
    
    /// <summary>
    /// Number of phases.
    /// </summary>
    public int NumPhases => Phases?.Length ?? 0;
    
    /// <summary>
    /// Current phase.
    /// </summary>
    public Phase CurrentPhase => Phases != null && PhaseIndex < Phases.Length 
        ? Phases[PhaseIndex] 
        : null;
    
    /// <summary>
    /// Check if this body is visible in the sky (between sunrise and sunset arcs).
    /// </summary>
    public bool IsVisible => Arc >= Common.SUNRISE_DEGREE && 
                             Arc <= Common.SUNSET_DEGREE + Sky.SKY_SPRITE_W;
    
    public AstralBody() { }
    
    public AstralBody(string tag, string name, int numPhases)
    {
        Tag = tag;
        Name = name;
        Phases = new Phase[numPhases];
    }
}
