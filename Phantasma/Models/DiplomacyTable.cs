using System;

namespace Phantasma.Models;

/// <summary>
/// Manages diplomatic relations between factions.
/// 
/// Each faction pair has a diplomacy level:
/// - Negative values = hostile
/// - Zero = neutral
/// - Positive values = allies
/// 
/// The table is symmetric: relations(A, B) == relations(B, A).
/// </summary>
public class DiplomacyTable
{
    public const int HostileThreshold = -2;
    public const int AlliesThreshold = 2;
    
    private readonly int[,] relations;
    private readonly int numFactions;
    
    public DiplomacyTable(int factions)
    {
        numFactions = factions;
        relations = new int[factions, factions];
    }
    
    public int Get(int f1, int f2) => relations[f1, f2];
    
    public void Set(int f1, int f2, int value)
    {
        relations[f1, f2] = value;
        relations[f2, f1] = value;  // Symmetric
    }

    public void Increment(int f1, int f2)
    {
        relations[f1, f2] += 1;
        relations[f2, f1] += 1;
    }

    public void Decrement(int f1, int f2)
    {
        relations[f1, f2] -= 1;
        relations[f2, f1] -= 1;
    }
    
    public bool AreHostile(int f1, int f2) => Get(f1, f2) <= HostileThreshold;
    public bool AreAllies(int f1, int f2) => Get(f1, f2) >= AlliesThreshold;
}
