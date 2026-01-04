using System;
using System.Collections.Generic;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    // ===================================================================
    // KERN-ADD API IMPLEMENTATIONS
    // These add status effects to characters.
    // ===================================================================
    
    public static object AddReveal(object characterObj, object durationObj)
    {
        // (kern-add-reveal character duration)
        // Grants ability to see invisible/hidden entities for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.RevealDuration = Math.Max(character.RevealDuration, duration);
                Console.WriteLine($"[AddReveal] {character.GetName()} gained Reveal for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-reveal: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-reveal: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddQuicken(object characterObj, object durationObj)
    {
        // (kern-add-quicken character duration)
        // Grants extra actions per turn for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.QuickenDuration = Math.Max(character.QuickenDuration, duration);
                Console.WriteLine($"[AddQuicken] {character.GetName()} gained Quicken for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-quicken: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-quicken: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddTimeStop(object characterObj, object durationObj)
    {
        // (kern-add-time-stop character duration)
        // Freezes other entities while this character can act for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 5);
            
            if (character != null)
            {
                character.TimeStopDuration = Math.Max(character.TimeStopDuration, duration);
                Console.WriteLine($"[AddTimeStop] {character.GetName()} gained Time Stop for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-time-stop: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-time-stop: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddMagicNegated(object characterObj, object durationObj)
    {
        // (kern-add-magic-negated character duration)
        // Prevents character from casting spells for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.MagicNegatedDuration = Math.Max(character.MagicNegatedDuration, duration);
                Console.WriteLine($"[AddMagicNegated] {character.GetName()} gained Magic Negated for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-magic-negated: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-magic-negated: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    public static object AddXrayVision(object characterObj, object durationObj)
    {
        // (kern-add-xray-vision character duration)
        // Grants ability to see through walls for duration turns.
        
        try
        {
            var character = characterObj as Character;
            int duration = Convert.ToInt32(durationObj ?? 10);
            
            if (character != null)
            {
                character.XrayVisionDuration = Math.Max(character.XrayVisionDuration, duration);
                Console.WriteLine($"[AddXrayVision] {character.GetName()} gained Xray Vision for {duration} turns");
            }
            else
            {
                RuntimeError("kern-add-xray-vision: null character");
            }
            
            return Builtins.Unspecified;
        }
        catch (Exception ex)
        {
            RuntimeError($"kern-add-xray-vision: {ex.Message}");
            return Builtins.Unspecified;
        }
    }
    
    /// <summary>
    /// (kern-add-spell type code level cost context flags range action-points (reagent-list))
    /// Adds a spell to the magic system indexed by its code (e.g., "AN" for An Nox).
    /// </summary>
    public static object AddSpell(object[] args)
    {
        int i = 0;
        
        // Required Parameters (0-7)
        object typeArg = args[i++];
        object codeArg = args[i++];
        object levelArg = args[i++];
        object costArg = args[i++];
        object contextArg = args[i++];
        object flagsArg = args[i++];
        object rangeArg = args[i++];
        
        // Last required - only increment if there's an optional param following.
        object actionPointsArg = i < args.Length - 1 ? args[i++] : args[i];
        
        // Optional Parameter
        object reagentsArg = i < args.Length ? args[i] : null;
        
        var typeTag = typeArg?.ToString()?.TrimStart('\'') ?? "";
        var code = codeArg?.ToString()?.ToUpperInvariant() ?? "";
    
        var objectType = Phantasma.GetRegisteredObject(typeTag) as ObjectType;
    
        int level = (int)Convert.ToDouble(levelArg ?? 0);
        int cost = (int)Convert.ToDouble(costArg ?? 0);
        int context = (int)Convert.ToDouble(contextArg ?? 0);
        int flags = (int)Convert.ToDouble(flagsArg ?? 0);
        int range = (int)Convert.ToDouble(rangeArg ?? 0);
        int actionPoints = (int)Convert.ToDouble(actionPointsArg ?? 0);
    
        // Parse reagent list.
        var reagents = new List<ObjectType>();
        if (reagentsArg is Cons cons)
        {
            foreach (var item in cons)
            {
                var reagentTag = item?.ToString();
                if (!string.IsNullOrEmpty(reagentTag))
                {
                    var reagent = Phantasma.GetRegisteredObject(reagentTag) as ObjectType;
                    if (reagent != null)
                        reagents.Add(reagent);
                }
            }
        }
    
        Magic.AddSpellByCode(code, objectType, level, cost, context, flags, range, actionPoints, reagents);
    
        Console.WriteLine($"  Added spell: {code} -> {typeTag} (level={level}, cost={cost})");
        return Builtins.Unspecified;
    }
}
