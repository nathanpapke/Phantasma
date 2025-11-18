namespace Phantasma.Models;

/// <summary>
/// Represents a position in the game world
/// </summary>
public class Location
{
    public Place Place { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
        
    public Location(Place place, int x, int y)
    {
        Place = place;
        X = x;
        Y = y;
    }
        
    public bool Equals(Location other)
    {
        return Place == other.Place && X == other.X && Y == other.Y;
    }
        
    public override bool Equals(object obj)
    {
        if (obj is Location other)
            return Equals(other);
        return false;
    }
        
    public override int GetHashCode()
    {
        return (Place?.GetHashCode() ?? 0) ^ X ^ Y;
    }
        
    public override string ToString()
    {
        return $"({X}, {Y}) in {Place?.Name ?? "nowhere"}";
    }
}