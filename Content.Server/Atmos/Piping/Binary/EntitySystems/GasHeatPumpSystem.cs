using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasHeatPumpSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasHeatPumpComponent, AtmosDeviceUpdateEvent>(OnHeatPumpUpdated);
        SubscribeLocalEvent<GasHeatPumpComponent, AtmosDeviceDisabledEvent>(OnHeatPumpLeaveAtmosphere);
        SubscribeLocalEvent<GasHeatPumpComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GasHeatPumpComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        SubscribeLocalEvent<GasHeatPumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasHeatPumpComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, GasHeatPumpComponent comp, ExaminedEvent args)
    {
        if (!Transform(uid).Anchored || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined",
            ("statusColor", "lightblue"),
            ("temp", $"{comp.TargetTemperature:0.##}")));

        if (comp.Blocked)
            args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined-blocked"));

        if (comp.TemperatureLocked)
            args.PushMarkup(Loc.GetString("gas-heat-pump-system-examined-locked"));
    }

    private void OnInit(EntityUid uid, GasHeatPumpComponent comp, ComponentInit args)
    {
        UpdateState(uid, comp);
    }

    private void OnPowerChanged(EntityUid uid, GasHeatPumpComponent comp, ref PowerChangedEvent args)
    {
        UpdateState(uid, comp);
    }

    private void OnHeatPumpLeaveAtmosphere(EntityUid uid, GasHeatPumpComponent comp, ref AtmosDeviceDisabledEvent args)
    {
        comp.Enabled = false;
        Dirty(uid, comp);
        UpdateState(uid, comp);
    }

    private void OnHeatPumpUpdated(EntityUid uid, GasHeatPumpComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!comp.Enabled || !_powerReceiverSystem.IsPowered(uid))
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        if (!_nodeContainer.TryGetNodes(uid, comp.RegulatedName, comp.ExternalName,
                out PipeNode? regulated, out PipeNode? external))
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        var previouslyBlocked = comp.Blocked;

        if (external.Air.Pressure < comp.MinPressure || regulated.Air.Pressure < comp.MinPressure)
        {
            comp.Blocked = true;
            if (previouslyBlocked != comp.Blocked)
                UpdateState(uid, comp);
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        comp.Blocked = false;
        if (previouslyBlocked != comp.Blocked)
            UpdateState(uid, comp);

        var tReg = regulated.Air.Temperature;
        var tExt = external.Air.Temperature;

		// Temp range locks out so we can avoid unbalanced weird stuff, no magic teg with this sorry
        var previouslyLocked = comp.TemperatureLocked;
        comp.TemperatureLocked =
            tReg < comp.MinOperatingTemperature || tReg > comp.MaxOperatingTemperature ||
            tExt < comp.MinOperatingTemperature || tExt > comp.MaxOperatingTemperature;
        if (previouslyLocked != comp.TemperatureLocked)
            UpdateState(uid, comp);
        if (comp.TemperatureLocked)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        var tTarget = comp.TargetTemperature;

        var delta = tReg - tTarget;

        if (MathF.Abs(delta) < Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            _ambientSoundSystem.SetAmbience(uid, false);
            return;
        }

        var coolingMode = delta > 0f;

        // Carnot COP capped by the max rate, tReg on top works for both heat and cool
        var tempGap = MathF.Max(MathF.Abs(tExt - tReg), 1f);
        var cop = comp.CarnotEfficiency * tReg / tempGap;
        var heatRate = MathF.Min(comp.MaxHeatTransferRate, cop * comp.WorkInput);

        // Joules moved this tick
        var joulesMoved = heatRate * args.dt;

        // Clamp to avoid going over/under the target temp
        var cReg = _atmosphereSystem.GetHeatCapacity(regulated.Air, true);
        var maxJoulesMoved = MathF.Abs(delta) * cReg;
        if (joulesMoved > maxJoulesMoved)
            joulesMoved = maxJoulesMoved;

        // Same amount leaves one side and enters the other
        // q+w version (to make heat waste from power instead of magic consumption): joulesMoved + joulesMoved / cop
        if (coolingMode)
        {
            _atmosphereSystem.AddHeat(regulated.Air, -joulesMoved);
            _atmosphereSystem.AddHeat(external.Air, joulesMoved);
        }
        else
        {
            _atmosphereSystem.AddHeat(regulated.Air, joulesMoved);
            _atmosphereSystem.AddHeat(external.Air, -joulesMoved);
        }

        _ambientSoundSystem.SetAmbience(uid, true);
    }

    private void OnPacketRecv(EntityUid uid, GasHeatPumpComponent comp, DeviceNetworkPacketEvent args)
    {
        if (!TryComp(uid, out DeviceNetworkComponent? netConn)
            || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
            return;

        var payload = new NetworkPayload();

        switch (cmd)
        {
            case AtmosDeviceNetworkSystem.SyncData:
                payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                payload.Add(AtmosDeviceNetworkSystem.SyncData, comp.ToAirAlarmData());
                _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload, device: netConn);
                return;

            case DeviceNetworkConstants.CmdSetState:
                if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out GasHeatPumpData? setData))
                    break;

                var previous = comp.ToAirAlarmData();

                if (previous.Enabled != setData.Enabled)
                {
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(uid)} {(setData.Enabled ? "enabled" : "disabled")}");
                }

                if (MathF.Abs(previous.TargetTemperature - setData.TargetTemperature) > 0.01f)
                {
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(uid)} target temperature changed from {previous.TargetTemperature:0.#} K to {setData.TargetTemperature:0.#} K");
                }

                comp.FromAirAlarmData(setData);
                Dirty(uid, comp);
                UpdateState(uid, comp);
                return;
        }
    }

    private void UpdateState(EntityUid uid, GasHeatPumpComponent comp, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (!comp.Enabled || !_powerReceiverSystem.IsPowered(uid))
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Off, appearance);
        else if (comp.Blocked || comp.TemperatureLocked)
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Blocked, appearance);
        else
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.On, appearance);
    }
}
