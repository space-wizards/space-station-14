using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Requires a random person to be killed, who must be a head.
/// If there are no heads it will fallback to any person.
/// </summary>
[DataDefinition]
public sealed partial class KillRandomHeadCondition : KillPersonCondition
{
    // TODO refactor all of this to be ecs
    public override IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
    {
        RequireDead = true;

        var allHumans = Minds.GetAliveHumansExcept(mindId);
        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        var allHeads = allHumans
            .Where(mind => Jobs.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
            .ToList();

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        return new KillRandomHeadCondition { TargetMindId = IoCManager.Resolve<IRobustRandom>().Pick(allHeads) };
    }
}
