using System;
using System.Collections.Generic;

namespace Phantasma.Models;

public class Sky
{
    // ===================================================================
    // CONSTANTS
    // ===================================================================
    
    /// <summary>
    /// Width of astral body sprites in pixels.
    /// </summary>
    public const int SKY_SPRITE_W = 16;  // TILE_W / 2
    
    /// <summary>
    /// Horizontal shift to make noon produce maximum light.
    /// </summary>
    private const int SKY_HORZ_SHIFT = Common.NOON_DEGREE - 90;  // 90
    
    // ===================================================================
    // COMPUTED FACTORS (calculated once at init)
    // ===================================================================
    
    /// <summary>
    /// Vertical shift for light function.
    /// </summary>
    private double _vertShift;
    
    /// <summary>
    /// Amplitude multiplier for light function.
    /// </summary>
    private double _amplitude;
    
    // ===================================================================
    // STATE
    // ===================================================================
    
    /// <summary>
    /// List of astral bodies, ordered by distance (farthest first).
    /// </summary>
    private readonly List<AstralBody> _bodies = new();
    
    /// <summary>
    /// Reference to the game clock.
    /// </summary>
    private Clock _clock;
    
    /// <summary>
    /// Public read-only access to astral bodies.
    /// </summary>
    public IReadOnlyList<AstralBody> Bodies => _bodies;
    
    // ===================================================================
    // EVENTS
    // ===================================================================
    
    /// <summary>
    /// Fired when ambient light level changes.
    /// </summary>
    public event Action<int> AmbientLightChanged;
    
    /// <summary>
    /// Fired when an astral body's phase changes.
    /// </summary>
    public event Action<AstralBody, int, int> PhaseChanged;
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    /// <summary>
    /// Initialize the sky system.
    /// </summary>
    public void Init(Clock clock)
    {
        _clock = clock;
        _bodies.Clear();
        ComputeFactors();
        
        Console.WriteLine("[Sky] Initialized");
    }
    
    /// <summary>
    /// Compute the light function factors.
    /// Called once at load time.
    /// </summary>
    private void ComputeFactors()
    {
        // The light function is: light = A * (sin(x - theta) + C)
        // 
        // Solve for C using sunrise time R where function = 0:
        // A * (sin(R - theta) + C) = 0
        // C = -sin(R - theta)
        
        _vertShift = -Math.Sin(DegreesToRadians(Common.SUNRISE_DEGREE - SKY_HORZ_SHIFT));
        
        // Solve for A (amplitude) using one hour after sunrise where function = 1.0:
        // A * (sin(V - theta) + C) = 1
        // A = 1 / (sin(V - theta) + C)
        
        double inverse = Math.Sin(DegreesToRadians(
            Common.SUNRISE_DEGREE + Common.DEGREES_PER_HOUR - SKY_HORZ_SHIFT)) + _vertShift;
        
        if (Math.Abs(inverse) < 0.0001)
            inverse = 0.0001;  // Avoid division by zero
            
        _amplitude = 1.0 / inverse;
    }
    
    /// <summary>
    /// Start a new session.
    /// </summary>
    public void StartSession(bool visible)
    {
        Advance(visible);
    }
    
    /// <summary>
    /// End the current session and clean up.
    /// </summary>
    public void EndSession()
    {
        _bodies.Clear();
    }
    
    // ===================================================================
    // ASTRAL BODY MANAGEMENT
    // ===================================================================
    
    /// <summary>
    /// Add an astral body to the sky.
    /// Bodies are kept sorted by distance (farthest first for proper rendering).
    /// </summary>
    public void AddAstralBody(AstralBody body)
    {
        // Insert in order by distance (farthest first).
        int insertIndex = 0;
        for (int i = 0; i < _bodies.Count; i++)
        {
            if (_bodies[i].Distance < body.Distance)
            {
                insertIndex = i;
                break;
            }
            insertIndex = i + 1;
        }
        
        _bodies.Insert(insertIndex, body);
        
        Console.WriteLine($"[Sky] Added astral body: {body.Name} (distance={body.Distance}, phases={body.NumPhases})");
    }
    
    /// <summary>
    /// Find an astral body by tag.
    /// </summary>
    public AstralBody GetBodyByTag(string tag)
    {
        return _bodies.Find(b => b.Tag == tag);
    }
    
    // ===================================================================
    // TIME ADVANCEMENT
    // ===================================================================
    
    /// <summary>
    /// Advance all astral bodies based on current clock time.
    /// </summary>
    public void Advance(bool visible)
    {
        if (_clock == null)
            return;
            
        int previousLight = GetAmbientLight();
        
        foreach (var body in _bodies)
        {
            AdvanceArc(body);
        }
        
        int newLight = GetAmbientLight();
        if (newLight != previousLight)
        {
            AmbientLightChanged?.Invoke(newLight);
        }
    }
    
    /// <summary>
    /// Advance an astral body's arc position.
    /// </summary>
    private void AdvanceArc(AstralBody body)
    {
        if (_clock == null || body.MinutesPerDegree == 0)
            return;
            
        // Calculate new arc position.
        int newArc = (int)(_clock.TotalMinutes / body.MinutesPerDegree);
        newArc += body.InitialArc;
        newArc %= 360;
        
        if (newArc == body.Arc)
            return;
            
        body.Arc = newArc;
        
        // Update phase if body has multiple phases.
        if (body.NumPhases > 0)
        {
            AdvancePhase(body);
        }
        
        // Update light output.
        int originalLight = body.Light;
        if (body.CurrentPhase != null)
        {
            body.Light = GetLightFromAstralBody(body.Arc, body.CurrentPhase.MaxLight);
        }
    }
    
    /// <summary>
    /// Advance an astral body's phase.
    /// </summary>
    private void AdvancePhase(AstralBody body)
    {
        if (_clock == null || body.NumPhases == 0)
            return;
            
        // Calculate new phase.
        int newPhase = 0;
        if (body.MinutesPerPhase > 0)
        {
            newPhase = (int)(_clock.TotalMinutes / body.MinutesPerPhase);
        }
        newPhase += body.InitialPhase;
        newPhase %= body.NumPhases;
        
        if (newPhase == body.PhaseIndex)
            return;
            
        int oldPhase = body.PhaseIndex;
        body.PhaseIndex = newPhase;
        
        // Fire phase change event.
        PhaseChanged?.Invoke(body, oldPhase, newPhase);
        
        // Execute callback if set.
        if (body.PhaseChangeCallback != null)
        {
            // TODO: Execute Scheme callback
            // closure_exec(body->gifc, "ypdd", "phase-change", body, oldPhase, newPhase);
        }
        
        Console.WriteLine($"[Sky] {body.Name} phase changed: {body.Phases[oldPhase]?.Name} -> {body.Phases[newPhase]?.Name}");
    }
    
    // ===================================================================
    // LIGHT CALCULATION
    // ===================================================================
    
    /// <summary>
    /// Calculate light output for an astral body at a given arc.
    /// Uses a modified sine wave to simulate day/night cycle.
    /// </summary>
    private int GetLightFromAstralBody(int arc, int maxLight)
    {
        // Light function: A * (sin(arc - theta) + C)
        // 
        // This creates a curve that:
        // - Starts ramping up at sunrise
        // - Reaches max around noon  
        // - Ramps down to zero at sunset
        // - Stays at zero during night
        
        int degrees = arc - SKY_HORZ_SHIFT;
        double radians = DegreesToRadians(degrees);
        double factor = _amplitude * (Math.Sin(radians) + _vertShift);
        factor = Math.Clamp(factor, 0.0, 1.0);
        
        int light = (int)(factor * maxLight);
        return Math.Max(0, light);
    }
    
    /// <summary>
    /// Get total ambient light from all astral bodies.
    /// </summary>
    public int GetAmbientLight()
    {
        int light = 0;
        
        foreach (var body in _bodies)
        {
            light += body.Light;
        }
        
        // TEMPORARY: Default to daytime if no astral bodies.
        if (_bodies.Count == 0)
            light = 200;  // Bright daylight
        
        return Math.Clamp(light, 0, Common.MAX_AMBIENT_LIGHT);
    }
    
    // ===================================================================
    // HELPERS
    // ===================================================================
    
    private static double DegreesToRadians(int degrees)
    {
        return degrees * 0.0174533;  // pi / 180
    }
    
    /// <summary>
    /// Convert arc position to pixel offset in sky window.
    /// Used for rendering astral body sprites.
    /// </summary>
    public int ArcToPixelOffset(int arc, int skyWidth)
    {
        // Linear mapping from arc degrees to pixel position.
        double slope = (double)(skyWidth + SKY_SPRITE_W) / 
                       (Common.SUNSET_DEGREE - Common.SUNRISE_DEGREE);
        double offset = -Common.SUNRISE_DEGREE * slope;
        
        return (int)(slope * arc + offset);
    }
    
    // ===================================================================
    // SAVE/LOAD
    // ===================================================================
    
    /// <summary>
    /// Save sky state to a save writer.
    /// </summary>
    public void Save(SaveWriter writer)
    {
        writer.WriteComment("---------");
        writer.WriteComment("Astronomy");
        writer.WriteComment("---------");
        
        foreach (var body in _bodies)
        {
            SaveAstralBody(writer, body);
        }
    }
    
    private void SaveAstralBody(SaveWriter writer, AstralBody body)
    {
        if (body.Gob != null)
        {
            writer.Enter("(bind-astral-body");
        }
        
        writer.Enter("(kern-mk-astral-body");
        writer.WriteLine($"'{body.Tag}\t; tag");
        writer.WriteLine($"\"{body.Name}\"\t; name");
        writer.WriteLine($"{body.Distance}\t; distance");
        writer.WriteLine($"{body.MinutesPerPhase}\t; minutes_per_phase");
        writer.WriteLine($"{body.MinutesPerDegree}\t; minutes_per_degree");
        writer.WriteLine($"{body.InitialArc}\t; initial_arc");
        writer.WriteLine($"{body.InitialPhase}\t; initial_phase");
        
        if (body.PhaseChangeCallback != null)
        {
            // TODO: Save closure
            writer.WriteLine("nil\t; gifc");
        }
        else
        {
            writer.WriteLine("nil\t; gifc");
        }
        
        writer.Enter("(list");
        foreach (var phase in body.Phases)
        {
            writer.WriteLine($"(list {phase.Sprite?.Tag ?? "nil"} {phase.MaxLight} \"{phase.Name}\")");
        }
        writer.Exit(")");
        writer.Exit(")");
        
        if (body.Gob != null)
        {
            // TODO: Save gob
            writer.Exit(") ;; bind-astral-body");
        }
    }
    
    public override string ToString()
    {
        return $"Sky({_bodies.Count} bodies, ambient={GetAmbientLight()})";
    }
}
