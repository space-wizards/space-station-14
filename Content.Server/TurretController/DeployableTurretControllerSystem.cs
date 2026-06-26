using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Access;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.TurretController;
using Content.Shared.Turrets;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.TurretController;

/// <inheritdoc/>
public sealed partial class DeployableTurretControllerSystem : SharedDeployableTurretControllerSystem
{
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployableTurretControllerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<DeployableTurretControllerComponent, DeviceListUpdateEvent>(OnDeviceListUpdate);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<TurretStatePayload>(OnPacketReceived);
    }

    private void OnBUIOpened(Entity<DeployableTurretControllerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUIState(ent);
    }

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

            _deviceNetwork.QueuePacket((ent.Owner, deviceNetwork), turretDeviceNetwork.Address, payload);
        }

        // Remove newly unlinked devices
        var turretsToRemove = args.OldDevices.Except(args.Devices);
        var refreshUi = false;

        foreach (var turretUid in turretsToRemove)
        {
            if (!TryComp<DeviceNetworkComponent>(turretUid, out var turretDeviceNetwork))
                continue;

            if (ent.Comp.LinkedTurrets.Remove(turretDeviceNetwork.Address))
                refreshUi = true;
        }

        if (refreshUi)
            UpdateUIState(ent);
    }

    private void OnPacketReceived(Entity<DeployableTurretControllerComponent> ent, ref TurretStatePayload payload, ref DeviceNetworkPacketData args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork) || deviceNetwork.ReceiveFrequency != args.Frequency)
            return;

        // If an update was received from a turret, connect to it and update the UI

        ent.Comp.LinkedTurrets[args.SenderAddress] = payload.State;
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

        _deviceNetwork.QueuePacket((ent.Owner, device), null, payload);
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

        _deviceNetwork.QueuePacket((ent.Owner, device), null, payload);
    }

    private void UpdateUIState(Entity<DeployableTurretControllerComponent> ent)
    {
        var turretStates = new Dictionary<string, string>();

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
