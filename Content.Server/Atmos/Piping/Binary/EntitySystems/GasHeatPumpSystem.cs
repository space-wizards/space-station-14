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
using Content.Shared.Atmos.Piping.Binary.Systems;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasHeatPumpSystem : SharedGasHeatPumpSystem
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
    }

    private void OnInit(Entity<GasHeatPumpComponent> ent, ref ComponentInit args)
    {
        UpdateState(ent);
    }

    private void OnPowerChanged(Entity<GasHeatPumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateState(ent);
    }

    private void OnHeatPumpLeaveAtmosphere(Entity<GasHeatPumpComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateState(ent);
    }

    private void OnHeatPumpUpdated(Entity<GasHeatPumpComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!ent.Comp.Enabled || !_powerReceiverSystem.IsPowered(ent))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        if (!_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.RegulatedName, ent.Comp.ExternalName,
                out PipeNode? regulated, out PipeNode? external))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        var previouslyBlocked = ent.Comp.Blocked;

        if (external.Air.Pressure < ent.Comp.MinPressure || regulated.Air.Pressure < ent.Comp.MinPressure)
        {
            ent.Comp.Blocked = true;
            if (previouslyBlocked != ent.Comp.Blocked)
                UpdateState(ent);
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        ent.Comp.Blocked = false;
        if (previouslyBlocked != ent.Comp.Blocked)
            UpdateState(ent);

        var tReg = regulated.Air.Temperature;
        var tExt = external.Air.Temperature;

        // Temp range locks out so we can avoid unbalanced weird stuff, no magic teg with this sorry
        var previouslyLocked = ent.Comp.TemperatureLocked;
        ent.Comp.TemperatureLocked =
            tReg < ent.Comp.MinOperatingTemperature || tReg > ent.Comp.MaxOperatingTemperature ||
            tExt < ent.Comp.MinOperatingTemperature || tExt > ent.Comp.MaxOperatingTemperature;
        if (previouslyLocked != ent.Comp.TemperatureLocked)
            UpdateState(ent);
        if (ent.Comp.TemperatureLocked)
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        var tTarget = ent.Comp.TargetTemperature;

        var delta = tReg - tTarget;

        if (MathF.Abs(delta) < Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        var coolingMode = delta > 0f;

        // Carnot COP capped by the max rate, tReg on top works for both heat and cool
        var tempGap = MathF.Max(MathF.Abs(tExt - tReg), 1f);
        var cop = ent.Comp.CarnotEfficiency * tReg / tempGap;
        var heatRate = MathF.Min(ent.Comp.MaxHeatTransferRate, cop * ent.Comp.WorkInput);

        // Joules moved this tick
        var joulesMoved = heatRate * args.dt;

        // Clamp regulated side: avoid going over/under the target temp.
        var cReg = _atmosphereSystem.GetHeatCapacity(regulated.Air, true);
        var maxJoulesMoved = MathF.Abs(delta) * cReg;
        if (joulesMoved > maxJoulesMoved)
            joulesMoved = maxJoulesMoved;

        // Clamp to avoid free energy creation (For corner case insane unrealistic heat pumps that can work at any temperature)
        var cExt = _atmosphereSystem.GetHeatCapacity(external.Air, true);
        var maxExternalJoules = coolingMode
            ? (Atmospherics.Tmax - tExt) * cExt // When cooling, we can't heat the sink over max temp
            : (tExt - Atmospherics.TCMB) * cExt; // When heating, don't draw energy past the minimum

        if (joulesMoved > maxExternalJoules)
            joulesMoved = maxExternalJoules;

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

        _ambientSoundSystem.SetAmbience(ent, true);
    }

    private void OnPacketRecv(Entity<GasHeatPumpComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!TryComp(ent, out DeviceNetworkComponent? netConn)
            || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
            return;

        var payload = new NetworkPayload();

        switch (cmd)
        {
            case AtmosDeviceNetworkSystem.SyncData:
                payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                payload.Add(AtmosDeviceNetworkSystem.SyncData, ToAirAlarmData(ent));
                _deviceNetSystem.QueuePacket(ent, args.SenderAddress, payload, device: netConn);
                return;

            case DeviceNetworkConstants.CmdSetState:
                if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out GasHeatPumpData? setData))
                    break;

                var previous = ToAirAlarmData(ent);

                if (previous.Enabled != setData.Enabled)
                {
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(ent)} {(setData.Enabled ? "enabled" : "disabled")}");
                }

                if (MathF.Abs(previous.TargetTemperature - setData.TargetTemperature) > 0.01f)
                {
                    _adminLogger.Add(LogType.AtmosDeviceSetting, LogImpact.Medium,
                        $"{ToPrettyString(ent)} target temperature changed from {previous.TargetTemperature:0.#} K to {setData.TargetTemperature:0.#} K");
                }

                FromAirAlarmData(ent, setData);
                Dirty(ent);
                UpdateState(ent);
                return;
        }
    }

    private void UpdateState(Entity<GasHeatPumpComponent> ent, AppearanceComponent? appearance = null)
    {
        if (!Resolve(ent, ref appearance, false))
            return;

        if (!ent.Comp.Enabled || !_powerReceiverSystem.IsPowered(ent))
            _appearance.SetData(ent, GasVolumePumpVisuals.State, GasVolumePumpState.Off, appearance);
        else if (ent.Comp.Blocked || ent.Comp.TemperatureLocked)
            _appearance.SetData(ent, GasVolumePumpVisuals.State, GasVolumePumpState.Blocked, appearance);
        else
            _appearance.SetData(ent, GasVolumePumpVisuals.State, GasVolumePumpState.On, appearance);
    }

    // Conversions
    public GasHeatPumpData ToAirAlarmData(Entity<GasHeatPumpComponent> ent)
    {
        return new GasHeatPumpData
        {
            Enabled = ent.Comp.Enabled,
            Dirty = false,
            IgnoreAlarms = false,
            TargetTemperature = ent.Comp.TargetTemperature,
            MinOperatingTemperature = ent.Comp.MinOperatingTemperature,
            MaxOperatingTemperature = ent.Comp.MaxOperatingTemperature,
        };
    }

    public void FromAirAlarmData(Entity<GasHeatPumpComponent> ent, GasHeatPumpData data)
    {
        ent.Comp.Enabled = data.Enabled;
        ent.Comp.TargetTemperature = data.TargetTemperature;
        Dirty(ent);
    }
}
