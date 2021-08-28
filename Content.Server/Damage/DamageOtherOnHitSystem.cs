using Content.Server.Damage.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;

namespace Content.Server.Damage
{
    public class DamageOtherOnHitSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        }

        private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
        {
            if (!args.Target.TryGetComponent(out IDamageableComponent? damageable))
                return;

            damageable.TryChangeDamage(component.DamageType, component.Amount, component.IgnoreResistances);
        }
    }
}
