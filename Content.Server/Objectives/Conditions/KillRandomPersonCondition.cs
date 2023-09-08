using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Requires a random person to be killed with no other conditions.
/// </summary>
[DataDefinition]
public sealed partial class KillRandomPersonCondition : KillPersonCondition
{
    public override IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
    {
        var allHumans = Minds.GetAliveHumansExcept(mindId);
        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        return new KillRandomPersonCondition {TargetMindId = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
    }
}
