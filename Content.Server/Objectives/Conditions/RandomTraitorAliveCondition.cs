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
                return entityMgr.TryGetComponent(mc.Mind?.OwnedEntity, out MobStateComponent mobState) &&
                       mc.Mind?.CharacterDeadIC == false &&
                       mc.Mind != mind &&
                       mc.Mind?.HasRole<TraitorRole>() == true;
            }).Select(mc => mc.Mind).ToList();
 
            return new RandomTraitorAliveCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allOtherTraitors)};
        }
    }
}
