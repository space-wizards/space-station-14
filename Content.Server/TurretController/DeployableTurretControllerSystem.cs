using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Access;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.TurretController;
using Content.Shared.Turrets;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceConfigurator;
using Content.Shared.DeviceConfigurator.Systems;
using Content.Shared.DeviceNetwork;

namespace Content.Server.TurretController;

/// <inheritdoc/>
public sealed partial class DeployableTurretControllerSystem : SharedDeployableTurretControllerSystem
{
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private DeviceListSystem _deviceList = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    [SubscribeLocalEvent]
    private void OnBUIOpened(Entity<DeployableTurretControllerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
            return;

        var payload = new TurretControllerRequestPayload();
        foreach (var (address, _) in _deviceList.GetDeviceList(ent.Owner))
        {
            _deviceNetwork.SendPacket((ent.Owner, deviceNetwork), address, ref payload);
        }

        UpdateUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnDeviceListUpdate(Entity<DeployableTurretControllerComponent> ent, ref DeviceListUpdateEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
            return;

        // List of new added turrets
        var turretsToAdd = args.Devices.Except(args.OldDevices);

        // Request data from newly linked devices
        var payload = new TurretControllerRequestPayload();

        foreach (var turretUid in turretsToAdd)
        {
            if (!HasComp<DeployableTurretComponent>(turretUid))
                continue;

            if (!TryComp<DeviceNetworkComponent>(turretUid, out var turretDeviceNetwork))
                continue;

            _deviceNetwork.SendPacket((ent.Owner, deviceNetwork), turretDeviceNetwork.Data.AddressId, ref payload);
        }

        // Remove newly unlinked devices
        var turretsToRemove = args.OldDevices.Except(args.Devices);
        var refreshUi = false;

        foreach (var turretUid in turretsToRemove)
        {
            if (!TryComp<DeviceNetworkComponent>(turretUid, out var turretDeviceNetwork))
                continue;

            if (ent.Comp.LinkedTurrets.Remove((turretDeviceNetwork.Data.AddressId, turretDeviceNetwork.Prefix)))
                refreshUi = true;
        }

        if (refreshUi)
            UpdateUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<DeployableTurretControllerComponent> ent, ref DeviceNetworkPacketEvent<TurretStatePayload> args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork) || deviceNetwork.Data.ReceiveFrequency != args.Frequency)
            return;

        // If an update was received from a turret, connect to it and update the UI

        ent.Comp.LinkedTurrets[(args.SenderAddress, deviceNetwork.Prefix)] = args.Data.State;
        UpdateUIState(ent);
    }

    protected override void ChangeArmamentSetting(Entity<DeployableTurretControllerComponent> ent, int armamentState, EntityUid? user = null)
    {
        base.ChangeArmamentSetting(ent, armamentState, user);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device))
            return;

        // Update linked turrets' armament statuses
        var payload = new TurretControllerSetArmamentPayload
        {
            ArmamentState = armamentState,
        };

        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user)} set {ToPrettyString(ent)} to {armamentState}");

        _deviceNetwork.SendPacket((ent.Owner, device), null, ref payload);
    }

    protected override void ChangeExemptAccessLevels(
        Entity<DeployableTurretControllerComponent> ent,
        HashSet<ProtoId<AccessLevelPrototype>> exemptions,
        bool enabled,
        EntityUid? user = null
    )
    {
        base.ChangeExemptAccessLevels(ent, exemptions, enabled, user);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device) ||
            !TryComp<TurretTargetSettingsComponent>(ent, out var turretTargetingSettings))
            return;

        // Update linked turrets' target selection exemptions
        var payload = new TurretControllerSetAccessPayload
        {
            AccessExemptions = turretTargetingSettings.ExemptAccessLevels,
        };

        foreach (var exemption in exemptions)
        {
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user)} set {ToPrettyString(ent)} authorization of {exemption} to {enabled}");
        }

        _deviceNetwork.SendPacket((ent.Owner, device), null, ref payload);
    }

    private void UpdateUIState(Entity<DeployableTurretControllerComponent> ent)
    {
        var turretStates = new Dictionary<LocDeviceAddress, string>();

        foreach (var (address, state) in ent.Comp.LinkedTurrets)
        {
            var stateName = state.ToString().ToLower();
            var stateDesc = Loc.GetString("turret-controls-window-turret-" + stateName);
            turretStates.Add(address, stateDesc);
        }

        var uiState = new DeployableTurretControllerBoundInterfaceState(turretStates);
        _userInterfaceSystem.SetUiState(ent.Owner, DeployableTurretControllerUiKey.Key, uiState);
    }
}
