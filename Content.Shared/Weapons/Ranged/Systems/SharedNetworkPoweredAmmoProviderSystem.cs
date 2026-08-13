using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using System.Numerics;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedNetworkPoweredAmmoProviderSystem : EntitySystem
{
    [Dependency] protected BatteryWeaponFireModesSystem FireMode = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<NetworkPoweredAmmoProviderComponent, ActivateInWorldEvent>(OnActivate, after: [typeof(ActivatableUISystem)]);
    }

    public virtual void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {

    }

    [SubscribeLocalEvent]
    private void OnToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, ref NetworkPoweredAmmoProviderToggleActiveMessage message)
    {
        if (TryComp(ent, out LockComponent? lockComp) && lockComp.Locked)
        {
            Popup.PopupEntity(Loc.GetString("comp-emitter-access-locked",
                ("target", ent.Owner)), ent, message.Actor);
            return;
        }

        ToggleActive(ent, message.Actor);
    }



    // stolen from gun system, do not merge lol
    protected IShootable EnsureShootable(EntityUid uid)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
            return cartridge;

        if (TryComp<HitscanAmmoComponent>(uid, out var hitscanAmmo))
            return hitscanAmmo;

        return EnsureComp<AmmoComponent>(uid);
    }

    private void OnActivate(Entity<NetworkPoweredAmmoProviderComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        ToggleActive(ent, args.User);
        args.Handled = true;
    }

    protected virtual void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {

    }
}
