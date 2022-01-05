using System.Collections.Generic;
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
            List<Mind.Mind> _allOtherTraitors = new List<Mind.Mind>();
            
            foreach (var targetMind in entityMgr.EntityQuery<MindComponent>())
            {
                if (targetMind.Mind?.CharacterDeadIC == false && targetMind.Mind != mind && targetMind.Mind?.HasRole<TraitorRole>() == true)
                {
                        _allOtherTraitors.Add(targetMind.Mind);
                }
            }
 
            return new RandomTraitorAliveCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(_allOtherTraitors)};
        }
    }
}
