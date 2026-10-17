using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Weapons.Ranged.Systems;

/// <inheritdoc/>>
public sealed partial class ServerNetworkPoweredAmmoProviderSystem : NetworkPoweredAmmoProviderSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;

    /// <summary> Turn on/off based on power feed. </summary>
    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!PowerState.GetWorkingState(ent.Owner))
            return;

        SetPoweredState(ent, !(args.ReceivedPower < args.DrawRate));
    }

    /// <summary> Turn on/off based on power feed. </summary>
    [SubscribeLocalEvent]
    private void OnApcChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref PowerChangedEvent args)
    {
        if (!PowerState.GetWorkingState(ent.Owner))
            return;

        SetPoweredState(ent, args.Powered);
    }

    /// <inheritdoc/>>
    public override void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        if (!TryComp<PowerStateComponent>(ent, out var powerState))
            return;

        PowerState.SetWorkingState((ent.Owner, powerState), true);

        SetPoweredState(ent, true);
    }

    /// <inheritdoc/>>
    public override void SwitchOff(Entity<NetworkPoweredAmmoProviderComponent> ent)
    {
        PowerState.SetWorkingState(ent.Owner, false);

        SetPoweredState(ent, false);
    }

    protected override void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
        {
            var isWorking = PowerState.GetWorkingState(ent.Owner);
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
                var stateText = isWorking ? "off" : "on";
                var logLevel = isWorking ? LogImpact.High : LogImpact.Medium;
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

    private void SetPoweredState(Entity<NetworkPoweredAmmoProviderComponent> ent, bool powerState)
    {
        if (ent.Comp.IsPowered == powerState)
            return;


        ent.Comp.IsPowered = powerState;
        Dirty(ent);

        if (!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        Gun.SetEnabled((ent, autoShoot), powerState);
    }
}
