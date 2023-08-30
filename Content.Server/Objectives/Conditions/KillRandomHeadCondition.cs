using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed partial class KillRandomHeadCondition : KillPersonCondition
{
    public override IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        RequireDead = true;

        var allHumans = EntityManager.EntityQuery<MindContainerComponent>(true).Where(mc =>
        {
            var entity = mc.Mind?.OwnedEntity;

            if (entity == default)
                return false;

            return EntityManager.TryGetComponent(entity, out MobStateComponent? mobState) &&
                  MobStateSystem.IsAlive(entity.Value, mobState) &&
                   mc.Mind != mind;
        }).Select(mc => mc.Mind).ToList();

        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        var allHeads = allHumans.Where(mind => mind?.AllRoles.Any(role => {
            if (role is not Job job)
                return false;

            // basically a command department check, pretty sussy but whatever
            return job.Prototype.RequireAdminNotify;
        }) ?? false).ToList();

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        return new KillRandomHeadCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHeads)};
    }

    public string Description => Loc.GetString("objective-condition-kill-head-description");
}
