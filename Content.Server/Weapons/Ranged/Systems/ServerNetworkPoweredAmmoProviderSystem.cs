using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Weapons.Ranged.Systems;

/// <inheritdoc/>>
public sealed partial class ServerNetworkPoweredAmmoProviderSystem : NetworkPoweredAmmoProviderSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;

    [SubscribeLocalEvent]
    private void OnOperatingChanged(Entity<NetworkPoweredAmmoProviderComponent> ent, ref OperatingStateChanged operatingState)
    {
        Gun.SetEnabled(ent.Owner, operatingState.IsOperating);
    }

    /// <inheritdoc/>>
    public override void SwitchOn(Entity<NetworkPoweredAmmoProviderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        PowerState.SetWorkingState(ent.Owner, true);
    }

    /// <inheritdoc/>>
    public override void SwitchOff(Entity<NetworkPoweredAmmoProviderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        PowerState.SetWorkingState(ent.Owner, false);
    }

    /// <inheritdoc/>>
    protected override void ToggleActive(Entity<NetworkPoweredAmmoProviderComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        if (TryComp<PhysicsComponent>(uid, out var phys) && phys.BodyType == BodyType.Static)
        {
            var isWorking = PowerState.GetWorkingState(ent.Owner);
            if (!isWorking)
            {
                SwitchOn(ent.AsNullable());
                Popup.PopupEntity(Loc.GetString("gun-toggle-on", ("target", uid)), uid, user);
            }
            else
            {
                SwitchOff(ent.AsNullable());
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
}
