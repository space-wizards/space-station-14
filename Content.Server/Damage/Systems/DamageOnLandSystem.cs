using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOnLandSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageOnLandComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<DamageOnLandComponent, LandEvent>(DamageOnLand);
        }

        private void HandleInit(EntityUid uid, DamageOnLandComponent component, ComponentInit args)
        {
            component.DamageType = _protoManager.Index<DamageTypePrototype>(component.DamageTypeId);
        }

        private void DamageOnLand(EntityUid uid, DamageOnLandComponent component, LandEvent args)
        {
            if (!ComponentManager.TryGetComponent<IDamageableComponent>(uid, out var damageable))
                return;

            damageable.TryChangeDamage(component.DamageType, component.Amount, component.IgnoreResistances);
        }
    }
}
