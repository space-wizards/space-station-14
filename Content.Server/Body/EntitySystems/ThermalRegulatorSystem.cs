using Content.Server.Body.Components;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    [UsedImplicitly]
    public class RespiratorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var respirator in ComponentManager.EntityQuery<RespiratorComponent>(false))
            {
                var Owner = respirator.Owner;
                if (Owner.TryGetComponent<IMobStateComponent>(out var state) &&
                    state.IsDead())
                {
                    return;
                }

                respirator.AccumulatedFrametime += frameTime;

                if (respirator.AccumulatedFrametime < 1)
                {
                    return;
                }

                ProcessGases(respirator.AccumulatedFrametime);
                ProcessThermalRegulation(respirator.AccumulatedFrametime);

                respirator.AccumulatedFrametime -= 1;

                if (SuffocatingPercentage() > 0)
                {
                    TakeSuffocationDamage();
                    return;
                }

                StopSuffocation();
            }
        }
    }
}
