namespace Phantasma.Models;

public struct Species
{
    public string Tag;
    // struct list
    public string Name;
    public int Str;
    public int Intl;
    public int Dex;
    public int Spd;
    public int Vr;
    // mmode mmode

    public int HpMod;   /* part of base hp contributed by species */
    public int HpMult;  /* additional hp per-level contributed by species */
    public int MpMod;   /* similar, for mana */
    public int MpMult;  /* similar, for mana */

    public Sprite SleepSprite;
    public int NSlots;
    public int Slots;
    public int NSpells;
    public int Spells;
    public ArmsType Weapon;
    public bool Visible;
    //sound_t DamageSound
    //sound_t MovementSound
    public int XpVal;           /* reward for killing this type */
    public string ArmorDice;    /* for scaly or chitinous types */
    public SkillSet Skills;
    public int Stationary;      /* doesn't move?                */
}