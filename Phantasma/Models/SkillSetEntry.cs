using System.Collections.Generic;

namespace Phantasma.Models;

/* These are used to list the skills in a skill set, and to associate a minimum
 * required level to use each skill. */
public struct SkillSetEntry
{
    public LinkedList<Skill> List;  /* list of other skills in the skill set */
    public Skill Skill;             /* the skill                             */
    public int Level;               /* min skill level to use this skill     */
    public int RefCount;            /* memory management                     */
}