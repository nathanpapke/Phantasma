using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

/// <summary>
/// Party - represents a group of characters (player party or NPC group).
/// The party is a collection/manager of Beings, not a Being itself.
/// The party's "position" is the position of its leader.
/// </summary>
public class Party
{
    private readonly List<Character> members = new List<Character>();
    
    // ====================================================================
    // PLAYER PARTY FEATURES
    // ====================================================================
    
    /// <summary>
    /// Shared inventory for the entire party.
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
    
    // ====================================================================
    // NPC PARTY FEATURES  
    // ====================================================================
    
    /// <summary>
    /// Schedule defining this party's daily routine (for NPCs).
    /// </summary>
    public Schedule? Schedule { get; set; }
    
    /// <summary>
    /// Current activity the party is performing.
    /// </summary>
    public Activity CurrentActivity { get; set; }
    
    /// <summary>
    /// Index of current appointment in schedule.
    /// </summary>
    public int CurrentAppointmentIndex { get; set; }
    
    /// <summary>
    /// Vehicle this party is in/on.
    /// </summary>
    public Vehicle? Vehicle { get; set; }
    
    /// <summary>
    /// Flag indicating this party wanders randomly.
    /// </summary>
    public bool IsWandering { get; set; }
    
    /// <summary>
    /// Flag indicating this party is waiting/idle at current location.
    /// </summary>
    public bool IsLoitering { get; set; }
    
    /// <summary>
    /// Custom AI controller function.
    /// </summary>
    public Action<Party>? Controller { get; set; }
    
    /// <summary>
    /// Faction ID for this party.
    /// </summary>
    public int Faction { get; set; }
    
    // Flee vector for combat AI.
    private int fleeX, fleeY;
    
    // ====================================================================
    // COMMON PROPERTIES
    // ====================================================================
    
    /// <summary>
    /// Get read-only list of party members.
    /// </summary>
    public IReadOnlyList<Character> Members => members;
    
    /// <summary>
    /// Size of the party (number of members).
    /// </summary>
    public int Size => members.Count;
    
    /// <summary>
    /// Flag to identify if this is the player's party.
    /// </summary>
    public bool IsPlayerParty { get; set; }
    
    // ====================================================================
    // DELEGATED PROPERTIES (from leader)
    // ====================================================================
    
    /// <summary>
    /// Party's position is the leader's position.
    /// </summary>
    public Location? Position
    {
        get => GetLeader()?.Position;
        set
        {
            var leader = GetLeader();
            if (leader != null && value != null)
            {
                leader.Position = value;
            }
        }
    }
    
    /// <summary>
    /// Party's action points come from the leader.
    /// </summary>
    public int ActionPoints
    {
        get => GetLeader()?.ActionPoints ?? 0;
        set
        {
            var leader = GetLeader();
            if (leader != null)
                leader.ActionPoints = value;
        }
    }
    
    /// <summary>
    /// Party's max action points come from the leader.
    /// </summary>
    public int MaxActionPoints => GetLeader()?.MaxActionPoints ?? 0;
    
    /// <summary>
    /// Party's place is where the leader is.
    /// </summary>
    public Place? Place => GetLeader()?.Position?.Place;
    
    // ====================================================================
    // CONSTRUCTORS
    // ====================================================================
    
    public Party()
    {
        Inventory = new Container();
        Gold = 0;
        Food = 0;
        CurrentActivity = Activity.Idle;
        CurrentAppointmentIndex = -1;
        IsWandering = false;
        IsLoitering = false;
        Faction = 0;
        IsPlayerParty = false;
    }
    
    // ====================================================================
    // MEMBER MANAGEMENT
    // ====================================================================
    
    /// <summary>
    /// Add a character to the party.
    /// </summary>
    public bool AddMember(Character character)
    {
        if (character == null)
            return false;
        
        if (!members.Contains(character))
        {
            members.Add(character);
            character.Party = this;
        }
        
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
    /// Get the leader (first member) of the party.
    /// </summary>
    public Character? GetLeader()
    {
        return members.FirstOrDefault();
    }
    
    /// <summary>
    /// Get the first living member.
    /// </summary>
    public Character? GetFirstLivingMember()
    {
        return members.FirstOrDefault(m => !m.IsDead());
    }
    
    /// <summary>
    /// Get the number of living (non-dead) members.
    /// </summary>
    public int GetNumLivingMembers()
    {
        return members.Count(m => !m.IsDead());
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
    /// Switch the order of two party members.
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
    
    // ====================================================================
    // INVENTORY MANAGEMENT
    // ====================================================================
    
    /// <summary>
    /// Add items to party inventory.
    /// </summary>
    public void Add(Item item)
    {
        Inventory.AddItem(item);
    }
    
    /// <summary>
    /// Remove items from party inventory.
    /// </summary>
    public void TakeOut(Item item)
    {
        Inventory.RemoveItem(item);
    }
    
    // ====================================================================
    // NPC AI BEHAVIOR
    // ====================================================================
    
    /// <summary>
    /// Execute the party's AI for this turn (NPC behavior).
    /// </summary>
    public void Exec()
    {
        if (IsDestroyed())
            return;
        
        // Don't run AI for player party.
        if (IsPlayerParty)
            return;
        
        StartTurn();
        
        while (ActionPoints > 0 && !IsDestroyed())
        {
            int initialPoints = ActionPoints;
            
            // Run the AI controller.
            if (Controller != null)
            {
                Controller(this);
            }
            else
            {
                // Run default behavior.
                DefaultAI();
            }
            
            // If we didn't use any action points, break.
            if (ActionPoints == initialPoints)
                break;
        }
        
        // Clear excess action points
        if (ActionPoints > 0)
            ActionPoints = 0;
    }
    
    /// <summary>
    /// Default AI behavior - follow schedule or wander.
    /// </summary>
    private void DefaultAI()
    {
        if (Schedule != null && Position?.Place != null)
        {
            FollowSchedule();
        }
        else if (IsWandering)
        {
            Wander();
        }
        else
        {
            ActionPoints = 0;
        }
    }
    
    /// <summary>
    /// Follow the party's schedule.
    /// </summary>
    private void FollowSchedule()
    {
        // TODO: Get current game time from clock system
        int currentHour = 12;
        int currentMinute = 0;
        
        var appointment = Schedule!.GetCurrentAppointment(currentHour, currentMinute);
        if (appointment == null)
        {
            if (IsWandering)
                Wander();
            else
                ActionPoints = 0;
            return;
        }
        
        CurrentActivity = appointment.Activity;
        
        if (appointment.ContainsPoint(Position!.X, Position.Y))
        {
            PerformActivity(appointment.Activity);
        }
        else
        {
            MoveTowardsAppointment(appointment);
        }
    }
    
    /// <summary>
    /// Move towards an appointment location.
    /// </summary>
    private void MoveTowardsAppointment(Appointment appointment)
    {
        int targetX = appointment.X + appointment.Width / 2;
        int targetY = appointment.Y + appointment.Height / 2;
        
        int dx = Math.Sign(targetX - Position!.X);
        int dy = Math.Sign(targetY - Position.Y);
        
        if (dx != 0 || dy != 0)
        {
            if (!Move(dx, dy))
            {
                if (dx != 0 && !Move(dx, 0))
                {
                    Move(0, dy);
                }
                else if (dy != 0)
                {
                    Move(0, dy);
                }
            }
        }
        else
        {
            IsLoitering = true;
            ActionPoints = 0;
        }
    }
    
    /// <summary>
    /// Perform the activity at the current location.
    /// </summary>
    private void PerformActivity(Activity activity)
    {
        switch (activity)
        {
            case Activity.Sleeping:
            case Activity.Eating:
                IsLoitering = true;
                ActionPoints = 0;
                break;
                
            case Activity.Working:
                if (new Random().Next(10) < 3)
                    Wander();
                else
                    ActionPoints = 0;
                break;
                
            case Activity.Idle:
                if (new Random().Next(10) < 5)
                    Wander();
                else
                    ActionPoints = 0;
                break;
                
            case Activity.Wandering:
                Wander();
                break;
                
            default:
                ActionPoints = 0;
                break;
        }
    }
    
    /// <summary>
    /// Wander randomly in a nearby direction.
    /// </summary>
    public void Wander()
    {
        if (Position?.Place == null)
        {
            ActionPoints = 0;
            return;
        }
        
        var random = new Random();
        int dx = random.Next(3) - 1;
        int dy = random.Next(3) - 1;
        
        if (dx == 0 && dy == 0)
        {
            dx = random.Next(2) == 0 ? -1 : 1;
        }
        
        if (!Move(dx, dy))
        {
            dx = random.Next(3) - 1;
            dy = random.Next(3) - 1;
            
            if (!Move(dx, dy))
            {
                ActionPoints = 0;
            }
        }
    }
    
    // ====================================================================
    // MOVEMENT
    // ====================================================================
    
    /// <summary>
    /// Move the party (moves the leader, others follow).
    /// </summary>
    public bool Move(int dx, int dy)
    {
        var leader = GetLeader();
        if (leader == null || leader.Position?.Place == null)
            return false;
        
        int newX = leader.Position.X + dx;
        int newY = leader.Position.Y + dy;
        
        // Check validity.
        if (leader.Position.Place.IsOffMap(newX, newY))
            return false;
        
        var terrain = leader.Position.Place.GetTerrain(newX, newY);
        if (terrain != null && !terrain.IsPassable)
            return false;
        
        // Check for hazards when wandering.
        if (IsWandering && leader.Position.Place.IsHazardous(newX, newY))
            return false;
        
        // Check movement cost.
        float cost = leader.Position.Place.GetMovementCost(newX, newY, leader);
        int apCost = (int)Math.Ceiling(cost);
        
        if (leader.ActionPoints < apCost)
            return false;
        
        // Move the leader.
        bool moved = leader.Move(dx, dy);
        
        // TODO: In future, move other members in formation
        
        return moved;
    }
    
    /// <summary>
    /// Start a new turn for the party.
    /// </summary>
    public void StartTurn()
    {
        IsLoitering = false;
        
        // Start turn for all members.
        foreach (var member in members)
        {
            member.StartTurn();
        }
    }
    
    // ====================================================================
    // COMBAT & DAMAGE
    // ====================================================================
    
    /// <summary>
    /// Apply damage to the party (distributes to a random member).
    /// </summary>
    public void Damage(int amount)
    {
        var livingMembers = members.Where(m => !m.IsDead()).ToList();
        if (livingMembers.Count > 0)
        {
            var target = livingMembers[new Random().Next(livingMembers.Count)];
            target.Damage(amount);
        }
    }
    
    /// <summary>
    /// Set the flee vector for combat AI.
    /// </summary>
    public void SetFleeVector(int x, int y)
    {
        fleeX = x;
        fleeY = y;
    }
    
    /// <summary>
    /// Get the flee vector.
    /// </summary>
    public void GetFleeVector(out int x, out int y)
    {
        x = fleeX;
        y = fleeY;
    }
    
    // ====================================================================
    // STATUS & INFO
    // ====================================================================
    
    /// <summary>
    /// Check if party is destroyed.
    /// </summary>
    public bool IsDestroyed()
    {
        return members.Count == 0 || AllDead();
    }
    
    /// <summary>
    /// Get the party's name (leader's name).
    /// </summary>
    public string GetName()
    {
        return GetLeader()?.GetName() ?? "Empty Party";
    }
    
    /// <summary>
    /// Get the party's sprite (leader's sprite).
    /// </summary>
    public Sprite? GetSprite()
    {
        return GetLeader()?.CurrentSprite;
    }
}
