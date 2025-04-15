using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

/// <summary>
/// Handles logic for pressure relief valves. Gas will only flow through the valve
/// if the pressure on the inlet side is over a certain pressure threshold.
/// See https://en.wikipedia.org/wiki/Relief_valve
/// </summary>
[UsedImplicitly]
public sealed class GasPressureReliefValveSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressureReliefValveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasPressureReliefValveComponent, AtmosDeviceUpdateEvent>(OnReliefValveUpdated);
        SubscribeLocalEvent<GasPressureReliefValveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasPressureReliefValveComponent, ActivateInWorldEvent>(OnValveActivate);

        // Bound UI subscriptions
        SubscribeLocalEvent<GasPressureReliefValveComponent, GasPressureReliefValveChangeThresholdMessage>(OnThresholdChangeMessage);
    }

    private void OnExamined(EntityUid valveEntityUid,
        GasPressureReliefValveComponent valveComponent,
        ExaminedEvent args)
    {
        // No cool stuff provided if it's unable to be examined.
        if (Transform(valveEntityUid).Anchored || !args.IsInDetailsRange)
            return;

        // TODO: Obliterate this shitcode, holy shit. Wanted to write out this proof of concept before I go play SK5.
        if (Loc.TryGetString("gas-pressure-relief-valve-system-examined",
                out var str,
                ("statusColor", valveComponent.Enabled ? "green" : "red"),
                ("open", valveComponent.Enabled)))
        {
            args.PushMarkup(str);
        }

        if (Loc.TryGetString("gas-pressure-relief-valve-examined-threshold-pressure",
                out var str2,
                ("threshold", $"{valveComponent.Threshold:0.#}")))
        {
            args.PushMarkup(str2);
        }

        if (Loc.TryGetString("gas-pressure-relief-valve-examined-flow-rate",
                out var str3,
                ("flowRate", $"{valveComponent.FlowRate:0.#}")))
        {
            args.PushMarkup(str3);
        }
    }

    private void OnInit(EntityUid valveEntityUid, GasPressureReliefValveComponent valveComponent, ComponentInit args)
    {
        UpdateAppearance(valveEntityUid, valveComponent);
    }

/// <summary>
/// Handles the updating logic for the pressure relief valve.
///
/// In summary, the valve should only open if the pressure on the inlet side is above a certain threshold.
/// When releasing gas, it should only release enough gas to tip under the threshold,
/// or the max atmospherics flow rate.
/// It should also not release more gas than it would take to equalize the pressure.
/// </summary>
/// <param name="valveEntityUid"> the <see cref="EntityUid"/> of the pressure relief valve</param>
/// <param name="valveComponent"> the <see cref="GasPressureReliefValveComponent"/> component of the valve</param>
/// <param name="args"> Args provided to us via <see cref="AtmosDeviceUpdateEvent"/></param>
    private void OnReliefValveUpdated(EntityUid valveEntityUid,
        GasPressureReliefValveComponent valveComponent,
        ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNodes(valveEntityUid,
                valveComponent.InletName,
                valveComponent.OutletName,
                out PipeNode? inletPipeNode,
                out PipeNode? outletPipeNode))
        {
            // Valve is not connected to any nodes, so we disable it.
            _ambientSoundSystem.SetAmbience(valveEntityUid, false);
            valveComponent.Enabled = false;
            return;
        }

        /*
        It's time for some math! :)

        Because this is SS14 and not a crazy accurate simulation of an actual PRV, we assume some things:
        1. The max transfer rate of the valve is limited
        2. We're not modeling springs or opening gaps or anything other than the above restriction

        Goals:
        1. Transfer enough gas to bring us below the pressure threshold
        2. Don't transfer more gas than the amount it would take to equalize the pressure
        (because it's a valve, not a pump)
        3. Don't transfer more than the max transfer rate of the valve
        */

        // Before we do anything else,
        // we check if the inlet pressure is above the threshold.
        var P1 = inletPipeNode.Air.Pressure;
        var P2 = outletPipeNode.Air.Pressure;

        // Inlet pressure is below the threshold, so we don't need to do anything.
        // We also check if the outlet pressure is higher than the inlet pressure,
        // as gas transfer is not possible in that case.
        if (P1 < valveComponent.Threshold || P2 > P1)
            return;

        // guess we're doing work now
        UpdateAppearance(valveEntityUid, valveComponent);
        _ambientSoundSystem.SetAmbience(valveEntityUid, true);
        valveComponent.Enabled = true;

        // Prepare the army!
        var n1 = inletPipeNode.Air.TotalMoles;
        var n2 = outletPipeNode.Air.TotalMoles;
        var V1 = inletPipeNode.Air.Volume;
        var V2 = outletPipeNode.Air.Volume;
        var T1 = inletPipeNode.Air.Temperature;
        var T2 = outletPipeNode.Air.Temperature;

        // First, calculate the amount of gas we need to transfer to bring us below the threshold.
        var deltaMolesToPressureThreshold = n1 - (valveComponent.Threshold * V1) / (Atmospherics.R * T1);

        // Second, calculate the moles required to equalize the pressure.
        var numerator = n1 * T1 * V2 - n2 * T2 * V1;
        var denominator = T2 * V1 + T1 * V2;
        var deltaMolesToEqualizePressure = numerator / denominator;

        // Third, make sure we only transfer the minimum of the two.
        var deltaMolesToTransfer = Math.Min(deltaMolesToPressureThreshold, deltaMolesToEqualizePressure);

        // Fourth, convert to the desired volume to transfer.
        var desiredVolumeToTransfer = deltaMolesToTransfer * ((Atmospherics.R * T1) / P1);

        // And finally, limit the transfer volume to the max flow rate of the valve.
        var actualVolumeToTransfer = Math.Min(desiredVolumeToTransfer,
            valveComponent.MaxTransferRate * _atmosphereSystem.PumpSpeedup() * args.dt);

        // We remove the gas from the inlet and merge it into the outlet.
        var removed = inletPipeNode.Air.RemoveVolume(actualVolumeToTransfer);
        _atmosphereSystem.Merge(outletPipeNode.Air, removed);

        // Oh, and set the flow rate (L/S) to the actual volume we transferred.
        // This is used for examine.
        valveComponent.FlowRate = desiredVolumeToTransfer / args.dt;
        // TODO: This math is wrong as shit. Figure out how to fix it later.
    }

    private void UpdateAppearance(EntityUid valveEntityUid,
        GasPressureReliefValveComponent? valveComponent = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(valveEntityUid, ref valveComponent, ref appearance, false))
            return;

        _appearance.SetData(valveEntityUid, FilterVisuals.Enabled, valveComponent.Enabled, appearance);
    }

    private void OnThresholdChangeMessage(EntityUid valveEntityUid,
        GasPressureReliefValveComponent valveComponent,
        GasPressureReliefValveChangeThresholdMessage args)
    {
        valveComponent.Threshold = Math.Max(0f, args.ThresholdPressure);
        _adminLogger.Add(LogType.AtmosVolumeChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure threshold on {ToPrettyString(valveEntityUid):device} to {args.ThresholdPressure}");
        DirtyUI(valveEntityUid, valveComponent);
    }

    private void DirtyUI(EntityUid valveEntityUid, GasPressureReliefValveComponent? valveComponent)
    {
        if (!Resolve(valveEntityUid, ref valveComponent))
            return;

        _userInterfaceSystem.SetUiState(valveEntityUid,
            GasPressureReliefValveUiKey.Key,
            new GasPressureReliefValveBoundUserInterfaceState(Name(valveEntityUid), valveComponent.Threshold));
    }

    private void OnValveActivate(EntityUid valveEntityUid, GasPressureReliefValveComponent valveComponent, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        if (Transform(valveEntityUid).Anchored)
        {
            _userInterfaceSystem.OpenUi(valveEntityUid, GasPressureReliefValveUiKey.Key, actor.PlayerSession);
            DirtyUI(valveEntityUid, valveComponent);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("comp-gas-pump-ui-needs-anchor"), args.User);
        }

        args.Handled = true;
    }


}
