using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Throwing;

namespace Content.Server.Damage.Systems
{
    public sealed partial class DamageOnLandSystem : EntitySystem
    {
        [Dependency] private DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageOnLandComponent, LandEvent>(DamageOnLand);
        }

        private void DamageOnLand(EntityUid uid, DamageOnLandComponent component, ref LandEvent args)
        {
            _damageableSystem.TryChangeDamage(uid, component.Damage, component.IgnoreResistances);
        }
    }
}
