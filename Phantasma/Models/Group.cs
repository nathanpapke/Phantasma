using System;

namespace Phantasma.Models;

/// <summary>
/// Group defines a subset of a PartyType - a group of members with
/// the same species, sprite, and generation parameters.
/// 
/// For example, a "goblin patrol" PartyType might have:
/// - Group 1: 2-4 goblin warriors (species=goblin, dice="2d2")
/// - Group 2: 0-1 goblin shaman (species=goblin_shaman, dice="1d2-1")
/// </summary>
public class Group
{
    /// <summary>
    /// Species of creatures in this group.
    /// </summary>
    public Species Species { get; set; }
    
    /// <summary>
    /// Sprite used to display creatures in this group.
    /// May differ from the species default (e.g., armored variant).
    /// </summary>
    public Sprite? Sprite { get; set; }
    
    /// <summary>
    /// Dice expression determining how many members to generate.
    /// Examples: "1d4" (1-4), "2d3" (2-6), "1d2+1" (2-3)
    /// </summary>
    public string Dice { get; set; } = "1";
    
    /// <summary>
    /// Scheme closure factory function to create group members.
    /// Called once per member to generate the actual Character.
    /// </summary>
    public object? Factory { get; set; }
    
    public Group()
    {
    }
    
    public Group(Species species, Sprite? sprite, string dice, object? factory)
    {
        Species = species;
        Sprite = sprite;
        Dice = dice ?? "1";
        Factory = factory;
    }
    
    /// <summary>
    /// Roll the dice to determine how many members this group generates.
    /// </summary>
    public int RollCount()
    {
        return Models.Dice.Roll(Dice);
    }
    
    public override string ToString()
    {
        return $"Group({Species.Name}: {Dice})";
    }
}
