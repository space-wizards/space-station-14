using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Recharges ammo whenever the gun is cycled.
/// </summary>
public sealed class RechargeCycleAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RechargeCycleAmmoComponent, ActivateInWorldEvent>(OnRechargeCycled);
    }

    private void OnRechargeCycled(Entity<RechargeCycleAmmoComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!TryComp<BasicEntityAmmoProviderComponent>(ent, out var basic) || args.Handled)
            return;

        if (basic.Count >= basic.Capacity || basic.Count == null)
            return;

        _gun.UpdateBasicEntityAmmoCount((ent, basic), basic.Count.Value + 1);
        Dirty(ent, basic);
        args.Handled = true;
    }
}
