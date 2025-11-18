namespace Phantasma.Models;

/// <summary>
/// Interface for objects that can block movement
/// Doors, gates, barriers, etc.
/// </summary>
public interface IBlockingObject
{
    bool IsPassable { get; }
}
