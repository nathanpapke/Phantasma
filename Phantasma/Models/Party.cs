using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

/// <summary>
/// Player's Party - a group of characters that share inventory and move together.
/// </summary>
public class Party
{
    private readonly List<Character> members = new List<Character>();
    
    /// <summary>
    /// Shared inventory for the entire party.
    /// All party members draw from this shared inventory.
    /// </summary>
    public Container Inventory { get; private set; }
    
    /// <summary>
    /// Gold carried by the party.
    /// </summary>
    public int Gold { get; set; }
    
    /// <summary>
    /// Food carried by the party.
    /// </summary>
    public int Food { get; set; }
    
    /// <summary>
    /// Get read-only list of party members.
    /// </summary>
    public IReadOnlyList<Character> Members => members;
    
    /// <summary>
    /// Size of the party (number of members).
    /// </summary>
    public int Size => members.Count;
    
    public Party() : base()
    {
        Inventory = new Container();
        Gold = 0;
        Food = 0;
    }
    
    /// <summary>
    /// Add a character to the party.
    /// </summary>
    public bool AddMember(Character character)
    {
        if (character == null)
            return false;
        
        members.Add(character);
        
        character.Party = this;
        
        // Order determines position in status display.
        
        // Make member loyal to party.
        
        return true;
    }
    
    /// <summary>
    /// Remove a character from the party.
    /// </summary>
    public void RemoveMember(Character character)
    {
        if (character == null)
            return;
        
        members.Remove(character);
        
        character.Party = null;
    }
    
    /// <summary>
    /// Get the number of living (non-dead) members.
    /// </summary>
    public int GetNumLivingMembers()
    {
        return members.Count(m => !m.IsDead());
    }
    
    /// <summary>
    /// Get the first living member (useful for single-member parties).
    /// </summary>
    public Character? GetFirstLivingMember()
    {
        return members.FirstOrDefault(m => !m.IsDead());
    }
    
    /// <summary>
    /// Get a member by their order/index in the party.
    /// </summary>
    public Character? GetMemberAtIndex(int index)
    {
        if (index < 0 || index >= members.Count)
            return null;
        return members[index];
    }
    
    /// <summary>
    /// Execute a function for each member.
    /// </summary>
    public void ForEachMember(Action<Character> action)
    {
        // Make a copy to avoid modification during iteration.
        foreach (var member in members.ToList())
        {
            action(member);
        }
    }
    
    /// <summary>
    /// Check if all members are dead.
    /// </summary>
    public bool AllDead()
    {
        return members.All(m => m.IsDead());
    }
    
    /// <summary>
    /// Add items to party inventory.
    /// </summary>
    public void Add(Item item)
    {
        Inventory.AddItem(item);
    }
    
    /// <summary>
    /// Remove items from party inventory.
    /// Convenience wrapper for Inventory.RemoveItem().
    /// </summary>
    public void TakeOut(Item item)
    {
        Inventory.RemoveItem(item);
    }
    
    /// <summary>
    /// Switch the order of two party members.
    /// Used by the New Order (N) command.
    /// </summary>
    public void SwitchOrder(Character ch1, Character ch2)
    {
        int index1 = members.IndexOf(ch1);
        int index2 = members.IndexOf(ch2);
        
        if (index1 >= 0 && index2 >= 0)
        {
            members[index1] = ch2;
            members[index2] = ch1;
        }
    }
}