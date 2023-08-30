using System.Linq;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed partial class KillRandomHeadCondition : KillPersonCondition
{
    // TODO refactor all of this to be ecs
    public override IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
    {
        RequireDead = true;

        var allHumans = EntityManager.EntityQuery<MindContainerComponent>(true).Where(mc =>
        {
            var entity = EntityManager.GetComponentOrNull<MindComponent>(mc.Mind)?.OwnedEntity;

            if (entity == default)
                return false;

            return EntityManager.TryGetComponent(entity, out MobStateComponent? mobState) &&
                   MobStateSystem.IsAlive(entity.Value, mobState) &&
                   mc.Mind != mindId;
        }).Select(mc => mc.Mind).ToList();

        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        var allHeads = allHumans
            .Where(mind => Jobs.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
            .ToList();

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        return new KillRandomHeadCondition { TargetMindId = IoCManager.Resolve<IRobustRandom>().Pick(allHeads) };
    }

    public string Description => Loc.GetString("objective-condition-kill-head-description");
}
