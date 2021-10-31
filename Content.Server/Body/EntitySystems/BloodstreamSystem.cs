using Content.Server.Body.Components;
using Content.Shared.MobState;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    public class BloodstreamSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var blood in EntityManager.EntityQuery<BloodstreamComponent>(false))
            {
                // TODO MIRROR BODY events
                var Owner = blood.Owner;
                if (Owner.TryGetComponent<IMobStateComponent>(out var state) &&
                    state.IsDead())
                {
                    return;
                }

                blood.AccumulatedFrametime += frameTime;

                // TODO unhardcode
                if (blood.AccumulatedFrametime < 1)
                {
                    return;
                }

                blood.ProcessGases(blood.AccumulatedFrametime);

                blood.AccumulatedFrametime -= 1;

                if (blood.SuffocatingPercentage() > 0)
                {
                    blood.TakeSuffocationDamage();
                    return;
                }

                blood.StopSuffocation();
            }
        }
    }
}
