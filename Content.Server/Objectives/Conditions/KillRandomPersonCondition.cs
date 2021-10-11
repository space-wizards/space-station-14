using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class KillRandomPersonCondition : KillPersonCondition
    {
        public override IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var allHumans = entityMgr.EntityQuery<MindComponent>(true).Where(mc =>
            {
                var entity = mc.Mind?.OwnedEntity;
                return (entity?.GetComponentOrNull<IMobStateComponent>()?.IsAlive() ?? false) && mc.Mind != mind;
            }).Select(mc => mc.Mind).ToList();
            return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
        }
    }
}
