using System;
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
}
