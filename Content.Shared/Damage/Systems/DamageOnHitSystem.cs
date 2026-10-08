using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnHitSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    /// <summary>
    /// Looks for a hit, then damages the held item an appropriate amount.
    /// </summary>
    [SubscribeLocalEvent]
    private void DamageItem(Entity<DamageOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Any())
            _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, ent.Comp.IgnoreResistances, origin: args.User);
    }
}
