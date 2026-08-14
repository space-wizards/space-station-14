using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnHitSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnHitComponent, MeleeHitEvent>(DamageItem);
    }

    /// <summary>
    /// Looks for a hit, then damages the held item an appropriate amount.
    /// </summary>
    private void DamageItem(Entity<DamageOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Any())
            _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.Damage, ent.Comp.IgnoreResistances, origin: args.User);
    }
}
