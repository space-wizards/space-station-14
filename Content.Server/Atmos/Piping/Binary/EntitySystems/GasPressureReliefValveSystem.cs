using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

/// <summary>
/// Handles serverside logic for pressure relief valves. Gas will only flow through the valve
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

    private void OnInit(Entity<GasPressureReliefValveComponent> valveEntity, ref ComponentInit args)
    {
        UpdateAppearance(valveEntity);
    }

    /// <summary>
    /// Handles the updating logic for the pressure relief valve.
    /// </summary>
    /// <param name="valveEntity"> the <see cref="Entity{T}" /> of the pressure relief valve</param>
    /// <param name="args"> Args provided to us via <see cref="AtmosDeviceUpdateEvent" /></param>
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

        Gas is simply transferred from the inlet to the outlet, restricted by flow rate and pressure.
        We want to transfer enough gas to bring the inlet pressure below the threshold,
        and only as much as our max flow rate allows.

        The equations:
        PV = nRT
        P1 = P2

        Can be used to calculate the amount of gas we need to transfer.
        */

        var P1 = inletPipeNode.Air.Pressure;
        var P2 = outletPipeNode.Air.Pressure;

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
        // We do this so that we don't accidentally transfer so much gas to the point
        // where the outlet pressure is higher than the inlet.
        var deltaMolesToTransfer = Math.Min(deltaMolesToPressureThreshold, deltaMolesToEqualizePressure);

        // Fourth, convert to the desired volume to transfer.
        var desiredVolumeToTransfer = deltaMolesToTransfer * ((Atmospherics.R * T1) / P1);

        // And finally, limit the transfer volume to the max flow rate of the valve.
        var actualVolumeToTransfer = Math.Min(desiredVolumeToTransfer,
            valveEntity.Comp.MaxTransferRate * _atmosphereSystem.PumpSpeedup() * args.dt);

        // We remove the gas from the inlet and merge it into the outlet.
        var removed = inletPipeNode.Air.RemoveVolume(actualVolumeToTransfer);
        _atmosphereSystem.Merge(outletPipeNode.Air, removed);

        // Calculate the flow rate in L/S for the UI.
        valveEntity.Comp.FlowRate = MathF.Round(actualVolumeToTransfer * args.dt, 1);
        DirtyField(valveEntity, valveEntity.Comp, nameof(valveEntity.Comp.FlowRate));

        if (valveEntity.Comp.PreviousValveState != valveEntity.Comp.Enabled)
        {
            // The valve state has changed since the last run, so we need to dirty it.
            valveEntity.Comp.PreviousValveState = valveEntity.Comp.Enabled;
            DirtyField(valveEntity, valveEntity.Comp, nameof(valveEntity.Comp.Enabled));
        }
    }

    /// <summary>
    /// Updates the visual appearance of the gas pressure relief valve based on its current state.
    /// </summary>
    /// <param name="valveEntityUid"> <see cref="EntityUid" /> of the valve.</param>
    /// <param name="valveComponent">
    /// The <see cref="GasPressureReliefValveComponent" /> of the entity. Will be resolved if not provided.
    /// </param>
    /// <param name="appearance">
    /// The <see cref="AppearanceComponent" /> associated with the entity. Will be resolved if not provided.
    /// <remarks>
    /// This isn't in shared because it's pointless to predict something whose state change is
    /// entirely handled by the server.
    /// </remarks>
    /// </param>
    private void UpdateAppearance(EntityUid valveEntityUid,
        GasPressureReliefValveComponent? valveComponent = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(valveEntityUid, ref valveComponent, ref appearance, false))
            return;

        _appearance.SetData(valveEntityUid,
            PressureReliefValveVisuals.State,
            valveComponent.Enabled,
            appearance);
    }
}
