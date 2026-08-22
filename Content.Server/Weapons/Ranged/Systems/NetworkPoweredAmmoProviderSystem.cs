using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class NetworkPoweredAmmoProviderSystem : SharedNetworkPoweredAmmoProviderSystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        SwitchOff(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<NetworkPoweredAmmoProviderComponent> ent, ref SignalReceivedEvent args)
    {
        // must anchor the emitter for signals to work
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
            if (component.IsOn)
            {
                SwitchOff(ent);
            }
            else
            {
                SwitchOn(ent);
            }
        }
    }

    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!ent.Comp.IsOn)
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
    
    protected override void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
        {
            if (!component.IsOn)
            {
                SwitchOn(ent);
                Popup.PopupEntity(Loc.GetString("comp-emitter-turned-on", ("target", uid)), uid, user);
            }
            else
            {
                SwitchOff(ent);
                Popup.PopupEntity(Loc.GetString("comp-emitter-turned-off", ("target", uid)), uid, user);
            }

            var stateText = component.IsOn ? "on" : "off";
            _adminLogger.Add(LogType.FieldGeneration, component.IsOn ? LogImpact.Medium : LogImpact.High, $"{ToPrettyString(user):player} toggled {ToPrettyString(uid):emitter} to {stateText}");
        }
        else
        {
            Popup.PopupEntity(Loc.GetString("comp-emitter-not-anchored", ("target", uid)), uid, user);
        }
    }

    [SubscribeLocalEvent]
    private void OnNetworkTakeAmmo(Entity<NetworkPoweredAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = args.Shots;
        if (shots == 0)
            return;

        if (!FireMode.TryGetFireMode((ent, null), out var fireMode))
            return;

        var entityCoordinates = args.Coordinates;

        for (var i = 0; i < shots; i++)
        {
            var ammo = SpawnAtPosition(fireMode.Prototype, entityCoordinates);
            args.Ammo.Add((ammo, EnsureShootable(ammo)));
        }
    }

    public void SwitchOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            return;

        powerConsumer.DrawRate = 1; // this needs to be not 0 so that the visuals still work.
        
        ent.Comp.IsOn = false;
        Dirty(ent);
        
        PowerOff(ent);
        UpdateAppearance(ent);
    }

    public override void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            return;

        if (!FireMode.TryGetFireMode(ent.Owner, out var fireMode))
            return;

        ent.Comp.IsOn = true;
        powerConsumer.DrawRate = fireMode.FireCost;

        Dirty(ent);
        PowerOn(ent);
        // Do not directly PowerOn().
        // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.

        UpdateAppearance(ent);
    }

    public void PowerOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!ent.Comp.IsPowered)
            return;

        if(!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        // AlertRadio((uid, component), component.LocUnpowered);

        ent.Comp.IsPowered = false;
        _gun.SetEnabled((ent, autoShoot), false);

        UpdateAppearance(ent);
    }

    public void PowerOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (ent.Comp.IsPowered)
            return;

        if(!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        ent.Comp.IsPowered = true;

        _gun.SetEnabled((ent, autoShoot), true);

        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        EmitterVisualState state;
        var component = ent.Comp;
        if (component.IsPowered)
        {
            state = EmitterVisualState.On;
        }
        else if (component.IsOn)
        {
            state = EmitterVisualState.Underpowered;
        }
        else
        {
            state = EmitterVisualState.Off;
        }
        _appearance.SetData(ent, EmitterVisuals.VisualState, state);
    }
}
