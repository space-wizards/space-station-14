using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Ammo provider that feeds on power line to provide ammo for gun.
/// Can be turned on/off, and be unpowered, but shoots only when turned on and powered properly.
/// <seealso cref="NetworkPoweredAmmoProviderComponent"/>
/// </summary>
public abstract partial class SharedNetworkPoweredAmmoProviderSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;

    /// <inheritdoc/>>
    public override void Initialize()
    {
        SubscribeLocalEvent<NetworkPoweredAmmoProviderComponent, ActivateInWorldEvent>(OnActivate, after: [typeof(ActivatableUISystem)]);
    }

    /// <summary> Toggles active for device. </summary>
    private void OnActivate(Entity<NetworkPoweredAmmoProviderComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp(ent, out LockComponent? lockComp) && lockComp.Locked)
        {
            Popup.PopupEntity(Loc.GetString("gun-access-locked", ("target", ent)), ent, args.User);
            return;
        }


        ToggleActive(ent, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Toggles device on or off, based on current state. Provides popup if device is locked (unlocking is required for proper interaction).
    /// </summary>
    /// <param name="ent">Device to toggle.</param>
    /// <param name="user">User that invoked action.</param>
    protected virtual void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {
        // no-op
    }
}
