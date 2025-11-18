using Content.Server.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Damage.Systems;

namespace Content.Server.Damage.Systems;

public sealed class DamageOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHitComponent, MeleeHitEvent>(DamageItem);
    }
    // Looks for a hit, then damages the held item an appropriate amount.
    private void DamageItem(EntityUid uid, DamageOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Any()) {
            _damageableSystem.TryChangeDamage(uid, component.Damage, component.IgnoreResistances);
        }
    }
}
