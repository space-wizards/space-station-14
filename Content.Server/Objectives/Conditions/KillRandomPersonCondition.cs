using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.SS220.TraitorComponentTarget;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class KillRandomPersonCondition : KillPersonCondition 
    {
        // функция выбора задачи убийства со списка всех игорьков
        public override IObjectiveCondition GetAssigned(Mind.Mind mind)
        {

            var allTargets = EntityManager.EntityQuery<TraitorTargetComponent>(true).Where(tc => {

                if (tc.Owner == default)
                    return false;

                if (EntityManager.IsQueuedForDeletion(tc.Owner))
                    return false;

                if (EntityManager.TryGetComponent<MindContainerComponent>(tc.Owner, out var mc))
                    if (mc.Mind == mind || mc.Mind == null)
                        return false;

                return EntityManager.TryGetComponent(tc.Owner, out MobStateComponent? mobState) &&
                    MobStateSystem.IsAlive(tc.Owner, mobState);
            }).Select(
                tc => {
                    EntityManager.TryGetComponent<MindContainerComponent>(tc.Owner, out var mc);
                    return mc?.Mind;
            }).ToList();

            // Проверка на кол-во найденных игроков на задачу убийства
            if (allTargets.Count == 0)
                return new DieCondition(); // I guess I'll die

            return new KillRandomPersonCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(allTargets) };
        }
    }
}
