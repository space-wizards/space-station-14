using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// The key ID used for the <see cref="UseDelaySystem"/> integration.
    /// </summary>
    public static string InsertionCooldownId = "InsertionCooldown";

    [SubscribeLocalEvent]
    private void OnCanAmmoInsertionEvent(Entity<AmmoProviderInsertionCooldownComponent> entity,
        ref CanAmmoInsertionEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = _useDelay.IsDelayed(entity.Owner, entity.Comp.UseDelayId);
    }

    [SubscribeLocalEvent]
    private void OnAmmoInsertionEvent(Entity<AmmoProviderInsertionCooldownComponent> entity,
        ref AmmoInsertionEvent args)
    {
        _useDelay.SetLength(entity.Owner, entity.Comp.InsertCooldown, entity.Comp.UseDelayId);
        _useDelay.TryResetDelay(entity.Owner, id: entity.Comp.UseDelayId);
    }
}
