using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Shared.MobState.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Server.Objectives.Interfaces;
using Content.Server.Traitor;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class RandomTraitorAliveCondition : OtherTraitorAliveCondition
    {
        public override IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var allOtherTraitors = entityMgr.EntityQuery<MindComponent>(true).Where(mc =>
            {
                var entity = mc.Mind?.OwnedEntity;
                
                if (entity == default)
                    return false;

                return entityMgr.TryGetComponent(entity, out MobStateComponent mobState) &&
                       mobState.IsAlive() &&
                       mc.Mind != mind &&
                       mc.Mind?.HasRole<TraitorRole>() == true;
            }).Select(mc => mc.Mind).ToList();
 
            return new RandomTraitorAliveCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allOtherTraitors)};
        }
    }
}
