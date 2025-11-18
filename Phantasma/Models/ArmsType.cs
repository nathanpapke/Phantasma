namespace Phantasma.Models;

public class ArmsType : ObjectType
{
    protected int SlotMasks;
    protected int NumHands;
    protected int Range;
    protected int Weight;
    protected int ModifierToApOfUser;
    protected bool Thrown;
    protected bool UbiquitousAmmo;
    protected string ArmorDice;
    protected string DamageDice;
    protected string ToDefendDice;
    protected string ToHitDice;
    //sound_t *fire_sound
    protected bool Beam;
    protected int StrAttackMod;
    protected int DexAttackMod;
    protected int CharDamageMod;
    protected float CharAvoidMod;
    protected Missile Missile;
    protected ObjectType AmmoType;

    public ArmsType(string tag, string name, Sprite sprite,
        int slotMasks,
        string toHitDice,
        string toDefendDice,
        int numHands,
        int range,
        int weight,
        string damageDice,
        string armorDice,
        int recActPts,
        int APMod,
        bool thrown,
        bool ubiquitousAmmo,
        //sound_t *firesound,
        MissileType missileType,
        ObjectType ammoType,
        int strAttackMod,
        int dexAttackMod,
        int charDamageMod,
        float charAvoidMod,
        bool isBeam)
    {
        // Initialize arms type.
    }
}