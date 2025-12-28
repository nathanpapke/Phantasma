using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// PartyType defines a template for creating NPC parties.
/// Contains groups of creatures that will be generated when
/// an instance of this party type is created.
/// 
/// Example usage in Scheme:
/// (kern-mk-party-type 'pt_goblin_patrol "Goblin Patrol" s_goblin formation
///   (list
///     (list species-goblin s_goblin "2d2" mk-goblin-warrior)
///     (list species-goblin-shaman s_shaman "1d2-1" mk-goblin-shaman)))
/// </summary>
public class PartyType : ObjectType
{
    /// <summary>
    /// List of groups that make up this party type.
    /// Each group generates a number of members based on its dice roll.
    /// </summary>
    private readonly List<Group> groups = new();
    
    /// <summary>
    /// Read-only access to groups.
    /// </summary>
    public IReadOnlyList<Group> Groups => groups;
    
    /// <summary>
    /// Formation used when this party enters combat.
    /// </summary>
    public Formation? Formation { get; set; }
    
    /// <summary>
    /// Sprite shown when party members are asleep.
    /// </summary>
    public Sprite? SleepSprite { get; set; }
    
    /// <summary>
    /// Maximum vision radius among all species in this party type.
    /// Computed when groups are added.
    /// </summary>
    public int VisionRadius { get; private set; }
    
    /// <summary>
    /// Whether any species in this party type is visible.
    /// </summary>
    public bool IsVisible { get; private set; }
    
    /// <summary>
    /// Minimum speed among all species in this party type.
    /// </summary>
    public int Speed { get; private set; } = int.MaxValue;
    
    // For group enumeration
    private int currentGroupIndex = -1;
    
    public PartyType() : base()
    {
        Layer = ObjectLayer.Being;
        VisionRadius = 0;
        IsVisible = false;
    }
    
    public PartyType(string tag, string name, Sprite? sprite)
        : base(tag, name, ObjectLayer.Being)
    {
        Sprite = sprite;
        VisionRadius = 0;
        IsVisible = false;
    }
    
    // ===================================================================
    // GROUP MANAGEMENT
    // ===================================================================
    
    /// <summary>
    /// Add a group to this party type.
    /// Updates vision radius, visibility, and speed based on species.
    /// </summary>
    public void AddGroup(Species species, Sprite? sprite, string dice, object? factory)
    {
        var group = new Group(species, sprite, dice, factory);
        groups.Add(group);
        
        // Update party type properties based on species.
        VisionRadius = Math.Max(VisionRadius, species.Vr);
        IsVisible = IsVisible || species.Visible;
        Speed = Math.Min(Speed, species.Spd > 0 ? species.Spd : Speed);
    }
    
    /// <summary>
    /// Add a pre-constructed group to this party type.
    /// </summary>
    public void AddGroup(Group group)
    {
        groups.Add(group);
        
        // Update party type properties based on species.
        VisionRadius = Math.Max(VisionRadius, group.Species.Vr);
        IsVisible = IsVisible || group.Species.Visible;
        Speed = Math.Min(Speed, group.Species.Spd > 0 ? group.Species.Spd : Speed);
    }
    
    // ===================================================================
    // GROUP ENUMERATION
    // ===================================================================
    
    /// <summary>
    /// Start enumerating groups. Returns the first group or null.
    /// </summary>
    public Group? EnumerateGroups()
    {
        currentGroupIndex = -1;
        return GetNextGroup();
    }
    
    /// <summary>
    /// Get the next group in enumeration. Returns null when done.
    /// </summary>
    public Group? GetNextGroup()
    {
        currentGroupIndex++;
        if (currentGroupIndex < groups.Count)
        {
            return groups[currentGroupIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Create a new Party instance from this type.
    /// </summary>
    public Party CreateInstance(int faction = 0, Vehicle? vehicle = null)
    {
        var party = new Party
        {
            Type = this,
            Faction = faction,
            Vehicle = vehicle,
            IsPlayerParty = false
        };
        
        // Set sprite from party type if available.
        if (Sprite != null)
        {
            party.Sprite = Sprite;
        }
        
        // Note: Members are created lazily or via CreateMembers().
        // The Scheme factory functions generate the actual characters.
        
        return party;
    }
    
    public override string ToString()
    {
        return $"PartyType({Tag}: {Name}, {groups.Count} groups)";
    }
}
