using Content.Shared.Damage.Components;
using Content.Shared.Throwing;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Damages the thrown item when it lands.
/// </summary>
public sealed class DamageOnLandSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnLandComponent, LandEvent>(DamageOnLand);
    }

    private void DamageOnLand(Entity<DamageOnLandComponent> ent, ref LandEvent args)
    {
        _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.Damage, ent.Comp.IgnoreResistances);
    }
}
