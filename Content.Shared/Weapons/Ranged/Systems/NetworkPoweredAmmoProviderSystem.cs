using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Ammo provider that feeds on power line to provide ammo for gun.
/// Can be turned on/off, and be unpowered, but shoots only when turned on and powered properly.
/// <seealso cref="NetworkPoweredAmmoProviderComponent"/>
/// </summary>
public abstract partial class NetworkPoweredAmmoProviderSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedGunSystem Gun = default!;
    [Dependency] protected SharedPowerStateSystem PowerState = default!;

    /// <summary> Toggles active for device. </summary>
    [SubscribeLocalEvent(after: [typeof(ActivatableUISystem)])]
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

    /// <summary> Turn off on unanchor. </summary>
    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        SwitchOff(ent);
    }

    /// <summary> Handle signals. </summary>
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<NetworkPoweredAmmoProviderComponent> ent, ref SignalReceivedEvent args)
    {
        // must anchor device for signals to work, its network powered after all!
        if (TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
            return;

        var component = ent.Comp;
        if (args.Port == component.OffPort)
        {
            SwitchOff(ent);
        }
        else if (args.Port == component.OnPort)
        {
            SwitchOn(ent);
        }
        else if (args.Port == component.TogglePort)
        {
            if (PowerState.GetWorkingState(ent.Owner))
            {
                SwitchOff(ent);
            }
            else
            {
                SwitchOn(ent);
            }
        }
    }


    /// <summary> Push selected ammo type to examine. </summary>
    [SubscribeLocalEvent]
    private void OnExamined(Entity<NetworkPoweredAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        var proto = ProtoMan.Index(ent.Comp.Prototype);
        args.PushMarkup(Loc.GetString("gun-selected-mode-examine", ("color", "yellow"), ("mode", proto.Name)));
    }


    /// <summary> Spawn ammo if we are ON. </summary>
    [SubscribeLocalEvent]
    private void OnNetworkTakeAmmo(Entity<NetworkPoweredAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = args.Shots;
        if (shots == 0 || !ent.Comp.IsPowered)
            return;

        var entityCoordinates = args.Coordinates;

        for (var i = 0; i < shots; i++)
        {
            var ammo = SpawnAtPosition(ent.Comp.Prototype, entityCoordinates);
            args.Ammo.Add((ammo, Gun.EnsureShootable(ammo)));
        }
    }

    /// <summary> Turns device ON, enabling drawing ammo by constantly using power. </summary>
    public virtual void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        // implementation can depend on power network so it is server-only
    }
    /// <summary>
    /// Turns device OFF, disabling power usage.
    /// </summary>
    public virtual void SwitchOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        // implementation can depend on power network so it is server-only
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
