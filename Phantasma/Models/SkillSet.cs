using System.Collections.Generic;

namespace Phantasma.Models;

/**
 * Skills are grouped into skill sets. Within a skill set, each skill has a
 * minimum level at which it may be used. Multiple skill sets may reference the
 * same skill, and each may have its own min level. So whereas skill structs
 * are shared across skill sets, skill_set_entry structs are unique to a skill
 * set. For example, a detect trap skill may be part of both a rogue skill set
 * and a ranger skill set, but the rogue can use the skill at level 2 whereas
 * the ranger must be level 4.
 */
public struct SkillSet
{
    public LinkedList<SkillSet> List;           /* list of all skill_sets in the session */
    public string Name;                         /* name of the skill set, eg "Ranger" */
    public LinkedList<SkillSetEntry> Skills;    /* list of skill_set_entry structs */
    public int RefCount;                        /* memory management */
}