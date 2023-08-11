using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class KillRandomPersonCondition : KillPersonCondition
    {
        public override IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var allHumans = new List<Mind.Mind>();
            var query = EntityManager.EntityQuery<MindContainerComponent, HumanoidAppearanceComponent>(true);
            while (query.MoveNext(out var mc, out _))
            {
                var entity = mc.Mind?.OwnedEntity;
                if (entity == default)
                    return false;

                if (EntityManager.TryGetComponent(entity, out MobStateComponent? mobState) &&
                    MobStateSystem.IsAlive(entity.Value, mobState) &&
                    mc.Mind != mind)
                {
                    allHumans.Add(mc.Mind);
                }
            }

            if (allHumans.Count == 0)
                return new DieCondition(); // I guess I'll die

            return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
        }
    }
}
