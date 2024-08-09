using Content.Shared.NPC.NuPC;
using Robust.Shared.Collections;

namespace Content.Server.NPC.NuPc;

public sealed partial class NpcGoalSystem
{
    public void GetGoal(ref ValueList<INpcGoal> goals, NpcKnowledgeComponent component, NpcChaseGoalGenerator generator)
    {
        if (component.LastHostileMobPositions.Count > 0)
        {
            goals.Add(new NpcChaseGoal());
        }
    }
}
