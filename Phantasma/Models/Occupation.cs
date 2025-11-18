using System.Collections.Generic;

namespace Phantasma.Models;

public struct Occupation
{
    // Use this struct as a LinkedListNode.
    public string Tag;
    public string Name;
    public float Magic;

    public int HpMod;
    public int HpMult;
    public int MpMod;
    public int MpMult;

    public int HitMod;
    public int DefMod;
    public int DamMod;
    public int ArmMod;

    public int XpVal;
    public int RefCount;
    
    public Gob Gob;
    //SkillSet skills;
}