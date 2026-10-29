using Content.Shared.Damage.Components;
using Content.Shared.Throwing;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Damages the thrown item when it lands.
/// </summary>
public sealed partial class DamageOnLandSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    [SubscribeLocalEvent]
    private void DamageOnLand(Entity<DamageOnLandComponent> ent, ref LandEvent args)
    {
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, ent.Comp.IgnoreResistances);
    }
}
