using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Formation defines positions for party members during combat.
/// When combat starts, party members are placed according to the formation.
/// </summary>
public class Formation
{
    /// <summary>
    /// Tag identifier for this formation.
    /// </summary>
    public string Tag { get; set; } = "";
    
    /// <summary>
    /// Array of position entries defining where each member stands.
    /// Index 0 = leader position, subsequent indices for other members.
    /// </summary>
    public FormationEntry[] Entries { get; set; } = Array.Empty<FormationEntry>();
    
    /// <summary>
    /// Number of positions in this formation.
    /// </summary>
    public int Count => Entries.Length;
    
    public Formation()
    {
    }
    
    public Formation(string tag, int numEntries)
    {
        Tag = tag;
        Entries = new FormationEntry[numEntries];
    }
    
    public Formation(string tag, FormationEntry[] entries)
    {
        Tag = tag;
        Entries = entries;
    }
    
    /// <summary>
    /// Get the position for a specific member index.
    /// Returns (0,0) if index is out of range.
    /// </summary>
    public (int x, int y) GetPosition(int memberIndex)
    {
        if (memberIndex >= 0 && memberIndex < Entries.Length)
        {
            return (Entries[memberIndex].X, Entries[memberIndex].Y);
        }
        return (0, 0);
    }
    
    /// <summary>
    /// Set the position for a specific member index.
    /// </summary>
    public void SetPosition(int memberIndex, int x, int y)
    {
        if (memberIndex >= 0 && memberIndex < Entries.Length)
        {
            Entries[memberIndex] = new FormationEntry(x, y);
        }
    }
    
    /// <summary>
    /// Create a default line formation (members in a row).
    /// </summary>
    public static Formation CreateLine(string tag, int size)
    {
        var entries = new FormationEntry[size];
        for (int i = 0; i < size; i++)
        {
            entries[i] = new FormationEntry(i, 0);
        }
        return new Formation(tag, entries);
    }
    
    /// <summary>
    /// Create a default column formation (members in a column).
    /// </summary>
    public static Formation CreateColumn(string tag, int size)
    {
        var entries = new FormationEntry[size];
        for (int i = 0; i < size; i++)
        {
            entries[i] = new FormationEntry(0, i);
        }
        return new Formation(tag, entries);
    }
    
    /// <summary>
    /// Create a square/block formation.
    /// </summary>
    public static Formation CreateSquare(string tag, int size)
    {
        var entries = new FormationEntry[size];
        int cols = (int)Math.Ceiling(Math.Sqrt(size));
        
        for (int i = 0; i < size; i++)
        {
            int x = i % cols;
            int y = i / cols;
            entries[i] = new FormationEntry(x, y);
        }
        return new Formation(tag, entries);
    }
    
    /// <summary>
    /// Get the default formation used when none is specified.
    /// </summary>
    public static Formation GetDefault()
    {
        // Default is a simple line formation for up to 8 members.
        return CreateLine("default", 8);
    }
    
    public override string ToString()
    {
        return $"Formation({Tag}: {Count} positions)";
    }
}
