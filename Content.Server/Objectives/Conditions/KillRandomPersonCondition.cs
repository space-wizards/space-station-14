using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed partial class KillRandomPersonCondition : KillPersonCondition
{
    public override IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
    {
        var allHumans = new List<EntityUid>();
        var query = EntityManager.EntityQuery<MindContainerComponent, HumanoidAppearanceComponent>(true);
        foreach (var (mc, _) in query)
        {
            var entity = EntityManager.GetComponentOrNull<MindComponent>(mc.Mind)?.OwnedEntity;
            if (entity == default)
                continue;

            if (EntityManager.TryGetComponent(entity, out MobStateComponent? mobState) &&
                MobStateSystem.IsAlive(entity.Value, mobState) &&
                mc.Mind != mindId && mc.Mind != null)
            {
                allHumans.Add(mc.Mind.Value);
            }
        }

        if (allHumans.Count == 0)
            return new DieCondition(); // I guess I'll die

        return new KillRandomPersonCondition {TargetMindId = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
    }
}
