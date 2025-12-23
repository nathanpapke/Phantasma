namespace Phantasma.Models;

/// <summary>
/// Represents a node in an A* pathfinding search.
/// </summary>
public class AStarNode
{
    public int X { get; set; }
    public int Y { get; set; }
    public int G { get; set; }              // Cost from start
    public int H { get; set; }              // Heuristic to goal
    public int F => G + H;                  // Total estimated cost
    public AStarNode? Parent { get; set; }  // Previous node in path
    
    public AStarNode(int x, int y, int g = 0, int h = 0, AStarNode? parent = null)
    {
        X = x;
        Y = y;
        G = g;
        H = h;
        Parent = parent;
    }
}
