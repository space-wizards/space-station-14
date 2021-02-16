using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Mobs.State;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    public class KillRandomPersonCondition : KillPersonCondition
    {
        public override IObjectiveCondition GetAssigned(Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var allHumans = entityMgr.ComponentManager.EntityQuery<MindComponent>(true).Where(mc =>
            {
                var entity = mc.Mind?.OwnedEntity;
                return (entity?.GetComponentOrNull<IMobStateComponent>()?.IsAlive() ?? false) && mc.Mind != mind;
            }).Select(mc => mc.Mind).ToList();
            return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allHumans)};
        }
    }
}
