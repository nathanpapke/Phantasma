namespace Phantasma.Models;

/// <summary>
/// Direction enumeration for movement and targeting.
/// Matches Nazghul's direction system.
/// </summary>
public enum Direction
{
    None = -1,       // DIRECTION_NONE
    NorthWest = 0,
    North = 1,
    NorthEast = 2,
    West = 3,
    Here = 4,
    East = 5,
    SouthWest = 6,
    South = 7,
    SouthEast = 8,
    Up = 9,
    Down = 10
}
