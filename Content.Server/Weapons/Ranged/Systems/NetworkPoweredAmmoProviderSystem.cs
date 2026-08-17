using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Weapons.Ranged.Systems;

/// <inheritdoc/>>
public sealed partial class NetworkPoweredAmmoProviderSystem : SharedNetworkPoweredAmmoProviderSystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private PowerStateSystem _powerState = default!;

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
            if (_powerState.GetWorkingState(ent.Owner))
            {
                SwitchOff(ent);
            }
            else
            {
                SwitchOn(ent);
            }
        }
    }

    /// <summary> Turn on/off based on power feed. </summary>
    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!_powerState.GetWorkingState(ent.Owner))
            return;

        if (args.ReceivedPower < args.DrawRate)
        {
            PowerOff(ent);
        }
        else
        {
            PowerOn(ent);
        }
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
            args.Ammo.Add((ammo, _gun.EnsureShootable(ammo)));
        }
    }

    /// <summary> Push selected ammo type to examine. </summary>
    [SubscribeLocalEvent]
    private void OnExamined(Entity<NetworkPoweredAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        var proto = ProtoMan.Index(ent.Comp.Prototype);
        args.PushMarkup(Loc.GetString("gun-selected-mode-examine", ("type", proto.Name)));
    }

    protected override void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
        {
            var isWorking = _powerState.GetWorkingState(ent.Owner);
            if (!isWorking)
            {
                SwitchOn(ent);
                Popup.PopupEntity(Loc.GetString("gun-toggle-on", ("target", uid)), uid, user);
            }
            else
            {
                SwitchOff(ent);
                Popup.PopupEntity(Loc.GetString("gun-toggle-off", ("target", uid)), uid, user);
            }

            if (ent.Comp.AdminLogToggleLevel.HasValue)
            {
                var stateText = isWorking ? "on" : "off";
                var logLevel = isWorking ? LogImpact.Medium : LogImpact.High;
                _adminLogger.Add(
                    ent.Comp.AdminLogToggleLevel.Value,
                    logLevel,
                    $"{ToPrettyString(user):player} toggled {ToPrettyString(uid):device} to {stateText}"
                );
            }
        }
        else
        {
            Popup.PopupEntity(Loc.GetString("gun-not-anchored", ("target", uid)), uid, user);
        }
    }

    /// <summary> Turns device ON, enabling drawing ammo by constantly using power. </summary>
    public void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!TryComp<PowerStateComponent>(ent, out var powerState))
            return;

        _powerState.SetWorkingState((ent.Owner, powerState), true);

        Dirty(ent);
        // Do not directly PowerOn().
        // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
    }

    private void SwitchOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        _powerState.SetWorkingState(ent.Owner, false);
        Dirty(ent);

        PowerOff(ent);
    }

    private void PowerOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!ent.Comp.IsPowered)
            return;


        ent.Comp.IsPowered = false;
        Dirty(ent);

        if (!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        _gun.SetEnabled((ent, autoShoot), false);
    }

    private void PowerOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (ent.Comp.IsPowered)
            return;

        ent.Comp.IsPowered = true;
        Dirty(ent);

        if (!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        _gun.SetEnabled((ent, autoShoot), true);
    }

}
