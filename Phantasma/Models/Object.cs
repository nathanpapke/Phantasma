using System;
using System.Collections.Generic;
using System.Linq;
using IronScheme.Runtime;

namespace Phantasma.Models;

/// <summary>
/// Base Class for All Game Objects (items, beings, etc.)
/// </summary>
public abstract class Object
{
    public abstract ObjectLayer Layer { get; } //removed set;
    
    private static int nextId = 1;
    
    public int Id { get; protected set; }
    public ObjectType Type { get; set; }
    public string Name { get; set; }
    public virtual Sprite Sprite { get; set;  }
    public Location Position { get; set; }
    public int PassabilityClass { get; set; } = 0;
    public int Count { get; set; } = 1;
    public int Light { get; set; } = 0;
    public Gob? Gob { get; set; }  // Scheme object reference
    
    private int visible = 1;
    
    /// <summary>
    /// Array of hook lists, one per hook type.
    /// </summary>
    protected List<Hook>[] hooks = new List<Hook>[(int)HookId.NumHooks];

    /// <summary>
    /// Condition string for status display ('G' good, 'P' poison, etc.).
    /// </summary>
    protected char[] condition = new char[8]; // OBJ_MAX_CONDITIONS

    /// <summary>
    /// Forces addEffect() to add without allowing "add-hook-hook" to block.
    /// </summary>
    protected bool forceEffect = false;
    
    public Object()
    {
        Id = nextId++;
        Position = new Location(null, 0, 0);
        
        for (int i = 0; i < (int)HookId.NumHooks; i++)
        {
            hooks[i] = new List<Hook>();
        }
    }
    
    public virtual bool Use(Being user)
    {
        return false;
    }
    
    public virtual bool Ready(Character character)
    {
        return false;
    }
    
    public Location GetPosition()
    {
        return Position;
    }
    
    public void SetPosition(Location loc)
    {
        Position = loc;
    }
    
    public virtual void SetPosition(Place place, int x, int y)
    {
        Position = new Location(place, x, y);
    }
    
    public virtual int GetX()
    {
        return Position.X;
    }
    
    public virtual int GetY()
    {
        return Position.Y;
    }
    
    public virtual Place? GetPlace()
    {
        return Position.Place;
    }
    
    public bool IsOnMap()
    {
        return Position.Place != null && 
               !Position.Place.IsOffMap(Position.X, Position.Y);
    }

    /// <summary>
    /// Check if this object is visible.
    /// </summary>
    public virtual bool IsVisible()
    {
        return visible > 0;
    }

    public void SetVisible(bool val)
    {
        if (val)
            visible++;
        else
            visible--;
    }
    
    public bool IsItem()
    {
        return Layer == ObjectLayer.Item;
    }
    
    public bool IsContainer()
    {
        return Layer == ObjectLayer.Container;
    }
    
    public bool IsGettable()
    {
        return Layer == ObjectLayer.Item;
    }
    
    public bool CanHandle()
    {
        return Layer == ObjectLayer.Mechanism;
    }
    
    public virtual void Handle(Character user)
    {
        // Override in mechanisms.
    }
    
    public virtual void Get(Character getter)
    {
        // Override in items.
    }

    public virtual Object? GetSpeaker()
    {
        // Override in party.
        
        return null;
    }

    public virtual void Remove()
    {
        if (Position.Place != null)
        {
            Position.Place.RemoveObject(this);
        }
    }

    /// <summary>
    /// Relocate this object to a new place and position.
    /// </summary>
    /// <param name="newPlace">Destination place</param>
    /// <param name="newX">Destination X coordinate</param>
    /// <param name="newY">Destination Y coordinate</param>
    /// <param name="cutscene">Optional cutscene closure to run during transition</param>
    public virtual void Relocate(Place newPlace, int newX, int newY, Callable? cutscene = null)
    {
        // Remove from current place.
        var oldPlace = GetPlace();
        if (oldPlace != null)
        {
            oldPlace.RemoveObject(this);
            oldPlace.Exit();
        }
    
        // Run cutscene if provided.
        if (cutscene != null)
        {
            cutscene.Call();
        }
    
        // Set new position.
        SetPosition(newPlace, newX, newY);
    
        // Add to new place.
        if (newPlace != null)
        {
            newPlace.AddObject(this, newX, newY);
            newPlace.Enter();
        }
    }
    
    /// <summary>
    /// Add an effect to this object.
    /// </summary>
    public virtual bool AddEffect(Effect effect, object? gob = null)
    {
        if (effect == null)
            return false;
        
        int hookId = (int)effect.HookId;
        if (hookId < 0 || hookId >= (int)HookId.NumHooks)
            return false;
        
        // Check if effect already exists and isn't cumulative.
        if (!effect.Cumulative && HasEffect(effect))
        {
            RemoveEffect(effect);
        }
        
        // Check if "add-hook" effects want to block this.
        if (!forceEffect && hooks[(int)HookId.AddHook].Count > 0)
        {
            // TODO: Run add-hook callbacks - they can return false to block.
        }
        
        // Calculate expiration.
        int expiration = effect.Duration; // Simplified; full impl uses clock
        
        // Add the hook.
        hooks[hookId].Add(new Hook(effect, gob, expiration));
        
        // Run the apply closure if present.
        if (effect.ApplyClosure != null)
        {
            // TODO: Call Scheme closure with (effect, this, gob).
        }
        
        // Update condition display.
        if (effect.StatusCode != ' ')
            SetCondition(effect.StatusCode);
        
        return true;
    }

    /// <summary>
    /// Restore an effect (used during save/load).
    /// </summary>
    public virtual void RestoreEffect(Effect effect, object? gob, int flags, int expiration)
    {
        if (effect == null)
            return;
        
        int hookId = (int)effect.HookId;
        if (hookId < 0 || hookId >= (int)HookId.NumHooks)
            return;
        
        hooks[hookId].Add(new Hook(effect, gob, expiration, flags));
        
        if (effect.RestartClosure != null)
        {
            // TODO: Call Scheme closure.
        }
    }

    /// <summary>
    /// Remove an effect from this object.
    /// </summary>
    public virtual bool RemoveEffect(Effect effect)
    {
        if (effect == null)
            return false;
        
        int hookId = (int)effect.HookId;
        if (hookId < 0 || hookId >= (int)HookId.NumHooks)
            return false;
        
        var hookList = hooks[hookId];
        int index = hookList.FindIndex(h => h.Effect == effect);
        
        if (index >= 0)
        {
            if (effect.RemoveClosure != null)
            {
                // TODO: Call Scheme closure.
            }
            
            hookList.RemoveAt(index);
            
            if (effect.StatusCode != ' ')
                ClearCondition(effect.StatusCode);
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Check if this object has a specific effect.
    /// </summary>
    public virtual bool HasEffect(Effect effect)
    {
        if (effect == null)
            return false;
        
        int hookId = (int)effect.HookId;
        if (hookId < 0 || hookId >= (int)HookId.NumHooks)
            return false;
        
        return hooks[hookId].Any(h => h.Effect == effect);
    }

    /// <summary>
    /// Run all effects for a specific hook.
    /// Iterates over a copy to allow safe modification.
    /// </summary>
    public virtual void RunHook(HookId hookId)
    {
        int id = (int)hookId;
        if (id < 0 || id >= (int)HookId.NumHooks)
            return;
        
        // Iterate over copy to allow modification during iteration.
        foreach (var hook in hooks[id].ToList())
        {
            if (hook.Effect.ExecClosure != null)
            {
                // TODO: Call Scheme closure with (effect, this, gob).
            }
            
            // TODO: Check expiration and remove if expired.
        }
    }

    /// <summary>
    /// Iterate over all hooks for a specific hook type.
    /// </summary>
    public virtual void HookForEach(HookId hookId, Action<Hook> callback)
    {
        int id = (int)hookId;
        if (id < 0 || id >= (int)HookId.NumHooks)
            return;
        
        // Iterate over copy for safety.
        foreach (var hook in hooks[id].ToList())
        {
            callback(hook);
        }
    }
    
    // ===================================================================
    // SOUND METHODS
    // ===================================================================
    
    /// <summary>
    /// Gets the movement sound for this object.
    /// Override in derived classes.
    /// </summary>
    public virtual Sound? GetMovementSound()
    {
        return null;
    }
    
    /// <summary>
    /// Plays movement sound at the given volume.
    /// </summary>
    public void PlayMovementSound(int volume)
    {
        var sound = GetMovementSound();
        if (sound != null)
        {
            SoundManager.Instance.Play(sound, volume);
        }
    }
    
    // ===================================================================
    // CONDITION METHODS
    // ===================================================================
    
    protected void SetCondition(char code)
    {
        for (int i = 0; i < condition.Length; i++)
        {
            if (condition[i] == '\0' || condition[i] == code)
            {
                condition[i] = code;
                return;
            }
        }
    }

    protected void ClearCondition(char code)
    {
        for (int i = 0; i < condition.Length; i++)
        {
            if (condition[i] == code)
            {
                for (int j = i; j < condition.Length - 1; j++)
                    condition[j] = condition[j + 1];
                condition[condition.Length - 1] = '\0';
                return;
            }
        }
    }

    public virtual string GetCondition()
    {
        int len = Array.IndexOf(condition, '\0');
        return len < 0 ? new string(condition) : new string(condition, 0, len);
    }

    public virtual void SetDefaultCondition()
    {
        Array.Clear(condition);
        condition[0] = 'G';
    }
}
