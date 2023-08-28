using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed partial class KillRandomPersonCondition : KillPersonCondition
{
    public override IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        var allHumans = new List<Mind.Mind>();
        var query = EntityManager.EntityQuery<MindContainerComponent, HumanoidAppearanceComponent>(true);
        foreach (var (mc, _) in query)
        {
            var entity = mc.Mind?.OwnedEntity;
            if (entity == default)
                continue;

            if (EntityManager.TryGetComponent(entity, out MobStateComponent? mobState) &&
                MobStateSystem.IsAlive(entity.Value, mobState) &&
                mc.Mind != mind && mc.Mind != null)
            {
                allHumans.Add(mc.Mind);
            }
        }

        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
    }
}
