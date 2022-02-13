using Content.Shared.Damage;
using Content.Server.Bed.Components;
using Content.Server.Buckle.Components;


namespace Content.Server.Bed
{
    public class BedSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var bedComponent in EntityManager.EntityQuery<BedComponent>())
            {
                var uid = bedComponent.Owner;
                if (!TryComp<StrapComponent>(uid, out var strapComponent))
                {
                    continue;
                }

                if (strapComponent.BuckledEntities.Count == 0)
                {
                    bedComponent.Accumulator = 0;
                    continue;
                }
                bedComponent.Accumulator += frameTime;

                if (bedComponent.Accumulator < bedComponent.HealTime)
                {
                    continue;
                }
                bedComponent.Accumulator -= bedComponent.HealTime;
                foreach (EntityUid healedEntity in strapComponent.BuckledEntities)
                {
                _damageableSystem.TryChangeDamage(healedEntity, bedComponent.Damage, true);
                }
            }
        }

    }
}
