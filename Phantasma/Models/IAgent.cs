using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Models;

// ============================================================================
// IAgent
//
// The interface that AI controllers implement to drive a Character each turn.
//
// Relationship to Character:
//   - There is no separate Actor class.
//   - In Phantasma, Character takes the role of ACTOR directly.
//     IAgent receives a Character named 'actor' and calls Character methods
//     through it.
//   - The agent does NOT replace the character. It controls it.
//
// Lifecycle (mirrors agent_new / agent_exec / agent_del):
//   - IAgent instances are created when an agent-controlled Character is
//     spawned and stored on that Character.
//   - Execute() is called by Session.HandleOtherBeings() once per turn,
//     replacing the Behavior.Execute(npc) call for agent-controlled
//     characters.
//   - The agent loops internally until actor.IsTurnEnded() is true.
//
// Perceived Session:
//   - perceivedSession is the agent's own belief-state Session, built
//     incrementally from what the agent has observed through its actor.
//   - It is NOT the real game session and contains only what the agent
//     has explicitly perceived and recorded across turns.
//   - To speculate before committing to an action, construct a throwaway
//     Session from the perceived session's current state and run the action
//     against it. Since no binder is attached and Start() is never called,
//     it is naturally headless — no rendering, no audio, no timers.
//
// Example:
//
//   public void Execute(Character actor, Session perceivedSession)
//   {
//       UpdateVmap(actor);
//
//       while (!actor.IsTurnEnded())
//       {
//           // Speculate: what happens if I move north?
//           var sim = new Session(
//               perceivedSession.CurrentPlace,
//               perceivedSession.Player,
//               perceivedSession.Party);
//           sim.HandlePlayerMove(0, -1);
//
//           if (sim.Player.HP >= perceivedSession.Player.HP)
//               MoveInDirection(actor, Direction.North);
//           else
//               actor.EndTurn();
//       }
//   }
// ============================================================================

public interface IAgent
{
    // ========================================================================
    // Lifecycle
    // ========================================================================
    
    /// <summary>
    /// Execute one turn for this agent.
    ///
    /// The agent must loop internally, calling the helper methods below,
    /// until actor.IsTurnEnded() is true or it calls actor.EndTurn().
    /// </summary>
    /// <param name="actor">
    /// The Character this agent controls.  Passed directly — no wrapper.
    /// </param>
    /// <param name="perceivedSession">
    /// The agent's belief-state session.  Built from prior perception,
    /// not a copy of the real session.  Use as the basis for sim sessions.
    /// </param>
    void Execute(Character actor, Session perceivedSession);
    
    // ========================================================================
    // Actor State Queries
    //
    // These are provided as default interface methods so implementations
    // can call them without boilerplate.
    // ========================================================================
    
    /// <summary>
    /// True if the actor has used all its action points this turn.
    /// </summary>
    static bool IsTurnOver(Character actor) =>
        actor.IsTurnEnded();
    
    /// <summary>
    /// Current hit points of the actor.
    /// </summary>
    static int HitPoints(Character actor) =>
        actor.HP;
    
    /// <summary>
    /// Maximum hit points of the actor.
    /// </summary>
    static int MaxHitPoints(Character actor) =>
        actor.MaxHP;
    
    /// <summary>
    /// Current mana points of the actor.
    /// </summary>
    static int ManaPoints(Character actor) =>
        actor.MP;
    
    /// <summary>
    /// Maximum mana points of the actor.
    /// </summary>
    static int MaxManaPoints(Character actor) =>
        actor.MaxMP;
    
    /// <summary>
    /// Remaining action points this turn.
    /// </summary>
    static int ActionPoints(Character actor) =>
        actor.ActionPoints;
    
    /// <summary>
    /// Maximum action points per turn.
    /// </summary>
    static int MaxActionPoints(Character actor) =>
        actor.MaxActionPoints;
    
    /// <summary>
    /// Current X position of the actor on its map.
    /// </summary>
    static int LocationX(Character actor) =>
        actor.GetX();
    
    /// <summary>
    /// Current Y position of the actor on its map.
    /// </summary>
    static int LocationY(Character actor) =>
        actor.GetY();
    
    /// <summary>
    /// True if the given map coordinate is within the actor's line of sight.
    /// </summary>
    static bool InLineOfSight(Character actor, int x, int y)
    {
        var place = actor.Position?.Place;
        if (place == null) return false;
 
        int distance = place.GetFlyingDistance(actor.GetX(), actor.GetY(), x, y);
        if (distance > actor.GetVisionRadius()) return false;
 
        return place.IsInLineOfSight(actor.GetX(), actor.GetY(), x, y);
    }
    
    /// <summary>
    /// True if the actor considers the given character hostile.
    /// </summary>
    static bool IsHostile(Character actor, Character other) =>
        Behavior.AreHostile(actor, other);
    
    // ========================================================================
    // Armament Queries
    // ========================================================================
    
    /// <summary>
    /// All weapons currently readied (equipped) by the actor.
    /// </summary>
    static IEnumerable<ArmsType> ReadiedArmaments(Character actor) =>
        actor.GetAllArms();
    
    /// <summary>
    /// All weapons in the actor's inventory, readied or not.
    /// </summary>
    static IEnumerable<ArmsType> InventoryArmaments(Character actor)
    {
        return actor.GetAllArms();
    }
    
    /// <summary>
    /// How many rounds of ammo remain for the given weapon.
    /// </summary>
    static int RoundsOfAmmo(Character actor, ArmsType weapon) =>
        actor.HasAmmo(weapon) ? 1 : 0;
    
    // ========================================================================
    // Spell and Item Queries
    // ========================================================================
    
    /// <summary>
    /// All spells the actor can currently cast.
    /// </summary>
    static IEnumerable<SpellType> CastableSpells(Character actor) =>
        actor.GetCastableSpells();
    
    /// <summary>
    /// All usable items in the actor's inventory.
    /// </summary>
    static IEnumerable<Item> UsableItems(Character actor)
    {
        var container = actor.GetInventoryContainer();
        if (container == null) return Enumerable.Empty<Item>();
        return container.Contents.Where(t => t.IsItem());
    }
    
    // ========================================================================
    // Actions
    // ========================================================================
    
    /// <summary>
    /// Move the actor one step in the given direction.
    /// </summary>
    static MoveResult MoveInDirection(Character actor, Direction dir)
    {
        if (actor.IsTurnEnded()) return MoveResult.TurnOver;
 
        int dx = Common.DirectionToDx(dir);
        int dy = Common.DirectionToDy(dir);
        return actor.Move(dx, dy) ? MoveResult.Okay : MoveResult.Impassable;
    }
    
    /// <summary>
    /// Attack a character at the given position with the given weapon.
    /// </summary>
    static AttackResult AttackBeing(Character actor, ArmsType weapon, Character target)
    {
        if (actor.IsTurnEnded()) return AttackResult.TurnOver;
        if (!actor.HasReadied(weapon)) return AttackResult.NotReadied;
        if (!actor.HasAmmo(weapon)) return AttackResult.NoAmmo;
 
        var place = actor.Position?.Place;
        if (place == null || target.Position?.Place != place)
            return AttackResult.NoTarget;
 
        int distance = place.GetFlyingDistance(
            actor.GetX(), actor.GetY(), target.GetX(), target.GetY());
        if (distance > weapon.Range) return AttackResult.TooFar;
 
        actor.Attack(weapon, target);
        return AttackResult.Okay;
    }
    
    /// <summary>
    /// Attack a map location with the given weapon (ranged or AoE).
    /// </summary>
    static AttackResult AttackLocation(Character actor, ArmsType weapon, int x, int y)
    {
        if (actor.IsTurnEnded()) return AttackResult.TurnOver;
        if (!actor.HasReadied(weapon)) return AttackResult.NotReadied;
        if (!actor.HasAmmo(weapon)) return AttackResult.NoAmmo;
 
        var place = actor.Position?.Place;
        if (place == null) return AttackResult.NoTarget;
 
        int distance = place.GetFlyingDistance(actor.GetX(), actor.GetY(), x, y);
        if (distance > weapon.Range) return AttackResult.TooFar;

        return AttackResult.NoTarget;
    }
    
    /// <summary>
    /// Pick up an object at the given position.
    /// </summary>
    static GetResult GetObject(Character actor, Object item)
    {
        if (actor.IsTurnEnded()) return GetResult.TurnOver;
 
        var place = actor.Position?.Place;
        if (place == null) return GetResult.NotApplicable;
 
        int distance = place.GetFlyingDistance(
            actor.GetX(), actor.GetY(), item.GetX(), item.GetY());
        if (distance > 1) return GetResult.TooFar;
 
        item.Get(actor);
        actor.DecreaseActionPoints(1);
        return GetResult.Okay;
    }
    
    /// <summary>
    /// Open a container at the given position.
    /// </summary>
    static OpenResult OpenContainer(Character actor, Container container)
    {
        if (actor.IsTurnEnded()) return OpenResult.TurnOver;
 
        var place = actor.Position?.Place;
        if (place == null) return OpenResult.NotApplicable;
 
        int distance = place.GetFlyingDistance(
            actor.GetX(), actor.GetY(), container.GetX(), container.GetY());
        if (distance > 1) return OpenResult.TooFar;
 
        container.Open();
        actor.DecreaseActionPoints(1);
        return OpenResult.Okay;
    }
    
    /// <summary>
    /// Operate a mechanism (door, lever, portcullis, etc.).
    /// </summary>
    static HandleResult HandleMechanism(Character actor, Object mechanism)
    {
        if (actor.IsTurnEnded()) return HandleResult.TurnOver;
        if (mechanism.Type?.CanHandle != true)
            return HandleResult.NotApplicable;
 
        var place = actor.Position?.Place;
        if (place == null) return HandleResult.NotApplicable;
 
        int distance = place.GetFlyingDistance(
            actor.GetX(), actor.GetY(), mechanism.GetX(), mechanism.GetY());
        if (distance > 1) return HandleResult.TooFar;
 
        mechanism.Handle(actor);
        actor.DecreaseActionPoints(1);
        return HandleResult.Okay;
    }
    
    /// <summary>
    /// Ready (equip) a weapon from inventory.
    /// </summary>
    static ReadyResult ReadyArms(Character actor, ArmsType weapon)
    {
        if (actor.IsTurnEnded()) return ReadyResult.TurnOver;
        return actor.Ready(weapon);
    }
    
    /// <summary>
    /// Unready (unequip) a weapon, returning it to inventory.
    /// </summary>
    static UnreadyResult UnreadyArms(Character actor, ArmsType weapon)
    {
        if (actor.IsTurnEnded()) return UnreadyResult.TurnOver;
        return actor.Unready(weapon) ? UnreadyResult.Okay : UnreadyResult.NotFound;
    }
    
    /// <summary>
    /// Use an item from inventory.
    /// </summary>
    static UseResult UseItem(Character actor, Object item)
    {
        if (actor.IsTurnEnded()) return UseResult.TurnOver;
        item.Use(actor);
        actor.DecreaseActionPoints(1);
        return UseResult.Okay;
    }
    
    /// <summary>
    /// Use an item aimed in a direction.
    /// </summary>
    static UseResult UseItemInDirection(Character actor, Object item, Direction dir)
    {
        if (actor.IsTurnEnded()) return UseResult.TurnOver;
        item.Use(actor);
        actor.DecreaseActionPoints(1);
        return UseResult.Okay;
    }
    
    /// <summary>
    /// Use an item on a specific character.
    /// </summary>
    static UseResult UseItemOnBeing(Character actor, Object item, Character target)
    {
        if (actor.IsTurnEnded()) return UseResult.TurnOver;
        item.Use(actor);
        actor.DecreaseActionPoints(1);
        return UseResult.Okay;
    }
    
    /// <summary>
    /// Use an item on a map location.
    /// </summary>
    static UseResult UseItemOnLocation(Character actor, Object item, int x, int y)
    {
        if (actor.IsTurnEnded()) return UseResult.TurnOver;
        item.Use(actor);
        actor.DecreaseActionPoints(1);
        return UseResult.Okay;
    }
    
    /// <summary>
    /// Cast a spell (self-targeted).
    /// </summary>
    static CastResult CastSpell(Character actor, SpellType spell)
    {
        if (actor.IsTurnEnded()) return CastResult.TurnOver;
        Magic.CastSpell(actor, spell, null);
        return CastResult.Okay;
    }
    
    /// <summary>
    /// Cast a spell aimed in a direction.
    /// </summary>
    static CastResult CastSpellInDirection(Character actor, SpellType spell, Direction dir)
    {
        if (actor.IsTurnEnded()) return CastResult.TurnOver;
        Magic.CastSpell(actor, spell, null);
        return CastResult.Okay;
    }
    
    /// <summary>
    /// Cast a spell on a specific character.
    /// </summary>
    static CastResult CastSpellOnBeing(Character actor, SpellType spell, Character target)
    {
        if (actor.IsTurnEnded()) return CastResult.TurnOver;
        Magic.CastSpell(actor, spell, target);
        return CastResult.Okay;
    }
    
    /// <summary>
    /// Cast a spell on a map location.
    /// </summary>
    static CastResult CastSpellOnLocation(Character actor, SpellType spell, int x, int y)
    {
        if (actor.IsTurnEnded()) return CastResult.TurnOver;
        Magic.CastSpell(actor, spell, (x, y));
        return CastResult.Okay;
    }
    
    /// <summary>
    /// Find a path from the actor's current position to the destination.
    /// Returns a sequence of directions to follow, or an empty array if
    /// no path exists.
    /// Requires A* search.
    /// </summary>
    static Direction[] FindPath(Character actor, int destX, int destY)
    {
        var place = actor.Position?.Place;
        if (place == null) return System.Array.Empty<Direction>();
        var nodes = AStar.Search(
            actor.GetX(), actor.GetY(), destX, destY,
            place.Width, place.Height,
            (x, y) => place.IsPassable(x, y, actor));
        if (nodes == null || nodes.Count < 2) return System.Array.Empty<Direction>();
        var dirs = new Direction[nodes.Count - 1];
        var node = nodes.First;
        for (int i = 0; i < dirs.Length; i++)
        {
            var next = node!.Next!;
            int dx = next.Value.X - node.Value.X;
            int dy = next.Value.Y - node.Value.Y;
            dirs[i] = (dx, dy) switch {
                (-1, -1) => Direction.NorthWest,
                ( 0, -1) => Direction.North,
                ( 1, -1) => Direction.NorthEast,
                (-1,  0) => Direction.West,
                ( 1,  0) => Direction.East,
                (-1,  1) => Direction.SouthWest,
                ( 0,  1) => Direction.South,
                ( 1,  1) => Direction.SouthEast,
                _        => Direction.None
            };
            node = next;
        }
        return dirs;
    }
    
    /// <summary>
    /// End the actor's turn, surrendering any remaining action points.
    /// </summary>
    static void EndTurn(Character actor) =>
        actor.EndTurn();
}
