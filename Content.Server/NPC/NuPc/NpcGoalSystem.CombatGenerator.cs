using Content.Shared.NPC.NuPC;
using Robust.Shared.Collections;

namespace Content.Server.NPC.NuPc;

public sealed partial class NpcGoalSystem
{
    public void GetGoal(ref ValueList<INpcGoal> goals, NpcKnowledgeComponent component, NpcCombatGoalGenerator generator)
    {
        if (component.HostileMobs.Count > 0)
        {
            goals.Add(new NpcCombatGoal());
        }
    }
}
