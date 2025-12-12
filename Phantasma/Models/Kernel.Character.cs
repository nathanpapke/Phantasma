using System;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// (kern-char-get-hp character)
    /// Gets current HP.
    /// </summary>
    public static object CharacterGetHp(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-hp: invalid character");
            return 0;
        }
        return c.HP;
    }

    /// <summary>
    /// (kern-char-get-max-hp character)
    /// Gets maximum HP.
    /// </summary>
    public static object CharacterGetMaxHp(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-max-hp: invalid character");
            return 0;
        }
        return c.MaxHP;
    }

    /// <summary>
    /// (kern-char-get-level character)
    /// Gets character level.
    /// </summary>
    public static object CharacterGetLevel(object character)
    {
        var c = character as Character;
        if (c == null)
        {
            LoadError("kern-char-get-level: invalid character");
            return 0;
        }
        return c.Level;
    }
    
    /// <summary>
    /// (kern-char-add-spell character spell)
    /// Add a spell to a character's known spells.
    /// </summary>
    public static object CharacterAddSpell(object character, object spell)
    {
        if (character is Character ch && spell is SpellType st)
        {
            ch.LearnSpell(st);
            return "#t".Eval();
        }
        
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-char-knows-spell? character spell)
    /// Check if a character knows a spell.
    /// </summary>
    public static object CharacterKnowsSpell(object character, object spell)
    {
        if (character is Character ch && spell is SpellType st)
        {
            return ch.KnowsSpell(st) ? "#t".Eval() : "#f".Eval();
        }
        
        return "#f".Eval();
    }
    
    /// <summary>
    /// (kern-cast-spell caster spell target)
    /// Cast a spell. Works with any session (including agent sessions).
    /// </summary>
    public static object CastSpell(object caster, object spell, object target)
    {
        if (caster is Character ch && spell is SpellType st)
        {
            bool success = Magic.CastSpell(ch, st, target);
            return success ? "#t".Eval() : "#f".Eval();
        }
        
        return "#f".Eval();
    }
}
