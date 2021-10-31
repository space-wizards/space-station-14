using Content.Server.Body.Components;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    [UsedImplicitly]
    public class ThermalRegulatorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var thermal in EntityManager.EntityQuery<ThermalRegulatorComponent>(false))
            {
                var Owner = thermal.Owner;
                // TODO MIRROR BODY events
                if (Owner.TryGetComponent<IMobStateComponent>(out var state) &&
                    state.IsDead())
                {
                    return;
                }

                thermal.AccumulatedFrametime += frameTime;

                // TODO unhardcode
                if (thermal.AccumulatedFrametime < 1)
                {
                    return;
                }

                thermal.ProcessThermalRegulation(thermal.AccumulatedFrametime);

                thermal.AccumulatedFrametime -= 1;
            }
        }
    }
}
