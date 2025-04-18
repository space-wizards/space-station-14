using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Visuals;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

/// <summary>
/// Handles logic for pressure relief valves. Gas will only flow through the valve
/// if the pressure on the inlet side is over a certain pressure threshold.
/// See https://en.wikipedia.org/wiki/Relief_valve
/// </summary>
[UsedImplicitly]
public sealed class GasPressureReliefValveSystem : SharedGasPressureReliefValveSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressureReliefValveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasPressureReliefValveComponent, AtmosDeviceUpdateEvent>(OnReliefValveUpdated);
    }
    // TODO!!!!!!!
    // TLDR this entire shit isn't predicted. The UI isn't predicted, the visuals aren't predicted, none of it is
    // predicted.
    // So it all needs to be changed to mirror the prediction done in the pressure pump.
    // Working implementation so far but it's a long ways off.

    private void OnInit(Entity<GasPressureReliefValveComponent> valveEntity, ref ComponentInit args)
    {
        UpdateAppearance(valveEntity);
    }

    /// <summary>
    /// Handles the updating logic for the pressure relief valve.
    ///
    /// In summary, the valve should only open if the pressure on the inlet side is above a certain threshold.
    /// When releasing gas, it should only release enough gas to tip under the threshold,
    /// or the max atmospherics flow rate.
    /// It should also not release more gas than it would take to equalize the pressure.
    /// </summary>
    /// <param name="valveEntity"> the <see cref="Entity{T}"/> of the pressure relief valve</param>
    /// <param name="args"> Args provided to us via <see cref="AtmosDeviceUpdateEvent"/></param>
    private void OnReliefValveUpdated(Entity<GasPressureReliefValveComponent> valveEntity,
        ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNodes(valveEntity.Owner,
                valveEntity.Comp.InletName,
                valveEntity.Comp.OutletName,
                out PipeNode? inletPipeNode,
                out PipeNode? outletPipeNode))
        {
            _ambientSoundSystem.SetAmbience(valveEntity, false);
            valveEntity.Comp.Enabled = false;
            UpdateAppearance(valveEntity);

            // Avoid it network spamming dirtying constantly by only checking if the state has actually changed.
            if (valveEntity.Comp.PreviousValveState != valveEntity.Comp.Enabled)
            {
                valveEntity.Comp.PreviousValveState = valveEntity.Comp.Enabled;
                DirtyField(valveEntity!, nameof(valveEntity.Comp.Enabled));
            }

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
        if (P1 <= valveEntity.Comp.Threshold || P2 > P1)
        {
            valveEntity.Comp.Enabled = false;
            _ambientSoundSystem.SetAmbience(valveEntity, false);
            UpdateAppearance(valveEntity);

            if (valveEntity.Comp.PreviousValveState != valveEntity.Comp.Enabled)
            {
                valveEntity.Comp.PreviousValveState = valveEntity.Comp.Enabled;
                valveEntity.Comp.FlowRate = 0;
                DirtyFields(valveEntity,
                    valveEntity.Comp,
                    MetaData(valveEntity),
                    nameof(valveEntity.Comp.FlowRate),
                    nameof(valveEntity.Comp.Enabled));
            }

            return;
        }

        // guess we're doing work now
        valveEntity.Comp.Enabled = true;
        _ambientSoundSystem.SetAmbience(valveEntity, true);
        UpdateAppearance(valveEntity);

        // Prepare the army!
        var n1 = inletPipeNode.Air.TotalMoles;
        var n2 = outletPipeNode.Air.TotalMoles;
        var V1 = inletPipeNode.Air.Volume;
        var V2 = outletPipeNode.Air.Volume;
        var T1 = inletPipeNode.Air.Temperature;
        var T2 = outletPipeNode.Air.Temperature;

        // First, calculate the amount of gas we need to transfer to bring us below the threshold.
        var deltaMolesToPressureThreshold = n1 - (valveEntity.Comp.Threshold * V1) / (Atmospherics.R * T1);

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
            valveEntity.Comp.MaxTransferRate * _atmosphereSystem.PumpSpeedup() * args.dt);

        // We remove the gas from the inlet and merge it into the outlet.
        var removed = inletPipeNode.Air.RemoveVolume(actualVolumeToTransfer);
        _atmosphereSystem.Merge(outletPipeNode.Air, removed);

        // Oh, and set the flow rate (L/S) to the actual volume we transferred.
        // This is used for player examine.
        valveEntity.Comp.FlowRate = MathF.Round(actualVolumeToTransfer * args.dt, 1);
        DirtyField(valveEntity, valveEntity.Comp, nameof(valveEntity.Comp.FlowRate));

        if (valveEntity.Comp.PreviousValveState != valveEntity.Comp.Enabled)
        {
            // The valve state has changed, so we need to dirty it.
            valveEntity.Comp.PreviousValveState = valveEntity.Comp.Enabled;
            DirtyField(valveEntity, valveEntity.Comp, nameof(valveEntity.Comp.Enabled));
        }
    }

    private void UpdateAppearance(EntityUid valveEntityUid,
        GasPressureReliefValveComponent? valveComponent = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(valveEntityUid, ref valveComponent, ref appearance, false))
            return;

        _appearance.SetData(valveEntityUid,
            PressureReliefValveVisuals.State,
            valveComponent.Enabled ? PressureReliefValveState.On : PressureReliefValveState.Off,
            appearance);
    }
}
