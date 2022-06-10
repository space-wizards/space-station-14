using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.MobState.Components;
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
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var allHumans = entityMgr.EntityQuery<MindComponent>(true).Where(mc =>
            {
                var entity = mc.Mind?.OwnedEntity;

                if (entity == default)
                    return false;

                return entityMgr.TryGetComponent(entity, out MobStateComponent? mobState) &&
                       mobState.IsAlive() &&
                       mc.Mind != mind;
            }).Select(mc => mc.Mind).ToList();

            if (allHumans.Count == 0)
                return new DieCondition(); // I guess I'll die

            return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
        }
    }
}
