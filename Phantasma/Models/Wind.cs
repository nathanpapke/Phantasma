using System;

namespace Phantasma.Models;

public class Wind
{
    // ===================================================================
    // STATE
    // ===================================================================
    
    private int _direction = Common.NORTH;
    private int _duration = 0;
    private readonly Random _random = new();
    
    // ===================================================================
    // PROPERTIES
    // ===================================================================
    
    /// <summary>
    /// Current wind direction (using Common direction constants).
    /// </summary>
    public int Direction
    {
        get => _direction;
        private set => _direction = value;
    }
    
    /// <summary>
    /// Turns remaining before wind might change.
    /// </summary>
    public int Duration
    {
        get => _duration;
        private set => _duration = value;
    }
    
    /// <summary>
    /// Wind direction as a string.
    /// </summary>
    public string DirectionString => Common.DirectionToString(_direction);
    
    // ===================================================================
    // EVENTS
    // ===================================================================
    
    /// <summary>
    /// Fired when wind direction changes.
    /// </summary>
    public event Action<int, int> DirectionChanged;
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    /// <summary>
    /// Initialize the wind system.
    /// </summary>
    public void Init()
    {
        _direction = Common.NORTH;
        _duration = 0;
        
        Console.WriteLine("[Wind] Initialized (direction: North)");
    }
    
    // ===================================================================
    // WIND CONTROL
    // ===================================================================
    
    /// <summary>
    /// Set wind direction and duration.
    /// </summary>
    public void SetDirection(int dir, int duration)
    {
        int oldDirection = _direction;
        _direction = dir;
        _duration = duration;
        
        if (oldDirection != _direction)
        {
            DirectionChanged?.Invoke(oldDirection, _direction);
            Console.WriteLine($"[Wind] Direction changed: {Common.DirectionToString(oldDirection)} -> {DirectionString}");
        }
    }
    
    /// <summary>
    /// Advance wind simulation by one turn.
    /// Wind may randomly change direction when duration expires.
    /// </summary>
    public void AdvanceTurns()
    {
        if (_duration > 0)
        {
            _duration--;
            return;
        }
        
        // Random chance to change direction
        if (_random.Next(100) < Common.WIND_CHANGE_PROBABILITY)
        {
            // Pick a random cardinal direction (N, S, E, W).
            int newDir = _random.Next(4) switch
            {
                0 => Common.NORTH,
                1 => Common.SOUTH,
                2 => Common.EAST,
                3 => Common.WEST,
                _ => Common.NORTH
            };
            
            SetDirection(newDir, 10);  // Default duration of 10 turns
        }
    }
    
    // ===================================================================
    // WIND EFFECTS
    // ===================================================================
    
    /// <summary>
    /// Calculate the effect of wind on movement in a given direction.
    /// Returns: positive = tailwind (bonus), negative = headwind (penalty), 0 = crosswind
    /// </summary>
    public int GetWindEffect(int movementDirection)
    {
        // Tailwind: moving in same direction as wind
        if (movementDirection == _direction)
            return 1;  // Tailwind bonus
            
        // Headwind: moving opposite to wind
        int opposite = GetOppositeDirection(_direction);
        if (movementDirection == opposite)
            return -1;  // Headwind penalty
            
        // Crosswind: no effect
        return 0;
    }
    
    /// <summary>
    /// Check if wind is favorable for sailing in a direction.
    /// </summary>
    public bool IsFavorable(int movementDirection)
    {
        return GetWindEffect(movementDirection) >= 0;
    }
    
    /// <summary>
    /// Get the opposite direction.
    /// </summary>
    private static int GetOppositeDirection(int dir)
    {
        return dir switch
        {
            Common.NORTH => Common.SOUTH,
            Common.SOUTH => Common.NORTH,
            Common.EAST => Common.WEST,
            Common.WEST => Common.EAST,
            Common.NORTHEAST => Common.SOUTHWEST,
            Common.SOUTHWEST => Common.NORTHEAST,
            Common.NORTHWEST => Common.SOUTHEAST,
            Common.SOUTHEAST => Common.NORTHWEST,
            _ => Common.NORTH
        };
    }
    
    // ===================================================================
    // SAVE/LOAD
    // ===================================================================
    
    /// <summary>
    /// Save wind state to a save writer.
    /// </summary>
    public void Save(SaveWriter writer)
    {
        writer.WriteLine($"(kern-set-wind {_direction} {_duration})");
    }
    
    public override string ToString()
    {
        return $"Wind: {DirectionString} (duration: {_duration})";
    }
}
