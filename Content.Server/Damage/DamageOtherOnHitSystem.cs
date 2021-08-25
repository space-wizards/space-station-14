using Content.Server.Damage.Components;
using Content.Shared.Damage;
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
            // Get damage from component, and apply to the target.
            RaiseLocalEvent(args.Target.Uid, new TryChangeDamageEvent(component.Damage, component.IgnoreResistances), false);
        }
    }
}
