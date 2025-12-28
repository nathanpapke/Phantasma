namespace Phantasma.Models;

/// <summary>
/// A single position entry in a formation.
/// </summary>
public struct FormationEntry
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public FormationEntry(int x, int y)
    {
        X = x;
        Y = y;
    }
}
