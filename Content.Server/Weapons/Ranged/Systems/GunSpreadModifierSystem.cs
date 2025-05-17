using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Weapons.Ranged.Systems;

/// <summary>
/// Ensures that GunSpreadModifierComponent works by listening to the GunGetAmmoSpreadEvent.
/// Also adds an examine message.
/// </summary>
public sealed class GunSpreadModifierSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunSpreadModifierComponent, GunGetAmmoSpreadEvent>(OnGunGetAmmoSpread);
    }

    private void OnGunGetAmmoSpread(EntityUid uid, GunSpreadModifierComponent comp, GunGetAmmoSpreadEvent args)
    {
        args.Spread *= comp.Spread;
    }
}
