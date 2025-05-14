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
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
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
            ChangeStatus(false, valveEntity);
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

        var p1 = inletPipeNode.Air.Pressure;
        var p2 = outletPipeNode.Air.Pressure;

        if (p1 <= valveEntity.Comp.Threshold || p2 >= p1)
        {
            ChangeStatus(false, valveEntity);
            return;
        }

        var t1 = inletPipeNode.Air.Temperature;

        // First, calculate the amount of gas we need to transfer to bring us below the threshold.
        var deltaMolesToPressureThreshold =
            AtmosphereSystem.MolesToPressureThreshold(inletPipeNode.Air, valveEntity.Comp.Threshold);

        // Second, calculate the moles required to equalize the pressure.
        // We round here to avoid the valve staying enabled for 0.00001 pressure differences.
        var deltaMolesToEqualizePressure =
            float.Round(_atmosphere.FractionToEqualizePressure(inletPipeNode.Air, outletPipeNode.Air) *
                        inletPipeNode.Air.TotalMoles,
                digits: 3,
                MidpointRounding.ToPositiveInfinity);

        // Third, make sure we only transfer the minimum of the two.
        // We do this so that we don't accidentally transfer so much gas to the point
        // where the outlet pressure is higher than the inlet.
        var deltaMolesToTransfer = Math.Min(deltaMolesToPressureThreshold, deltaMolesToEqualizePressure);

        // Fourth, convert to the desired volume to transfer.
        var desiredVolumeToTransfer = deltaMolesToTransfer * ((Atmospherics.R * t1) / p1);

        // And finally, limit the transfer volume to the max flow rate of the valve.
        var actualVolumeToTransfer = Math.Min(desiredVolumeToTransfer,
            valveEntity.Comp.MaxTransferRate * _atmosphere.PumpSpeedup() * args.dt);

        // We remove the gas from the inlet and merge it into the outlet.
        var removed = inletPipeNode.Air.RemoveVolume(actualVolumeToTransfer);
        _atmosphere.Merge(outletPipeNode.Air, removed);

        // Calculate the flow rate in L/s for the UI.
        valveEntity.Comp.FlowRate = MathF.Round(actualVolumeToTransfer * args.dt, 1);
        DirtyField(valveEntity, valveEntity.Comp, nameof(valveEntity.Comp.FlowRate));

        ChangeStatus(true, valveEntity);
    }

    /// <summary>
    /// Updates the visual appearance of the valve based on its current state.
    /// </summary>
    /// <param name="valveEntity">The <see cref="Entity{GasPressureReliefValveComponent, AppearanceComponent}"/>
    /// representing the valve with respective components.</param>
    private void UpdateAppearance(Entity<GasPressureReliefValveComponent, AppearanceComponent?> valveEntity)
    {
        if (!Resolve(valveEntity, ref valveEntity.Comp2, false))
            return;

        _appearance.SetData(valveEntity,
            PressureReliefValveVisuals.State,
            valveEntity.Comp1.Enabled);
    }

    /// <summary>
    /// Updates the valve's appearance and sound based on its current state, while
    /// also preventing network spamming.
    /// </summary>
    /// <param name="enabled">The new state to set on the valve</param>
    /// <param name="valveEntity">The valve to update</param>
    private void ChangeStatus(bool enabled, Entity<GasPressureReliefValveComponent> valveEntity)
    {
        // We need to prevent spamming the network with updates, so only check if we've
        // switched states.
        if (valveEntity.Comp.Enabled != enabled)
        {
            valveEntity.Comp.Enabled = enabled;
            _ambientSound.SetAmbience(valveEntity, enabled);
            UpdateAppearance(valveEntity);

            if (!enabled)
            {
                valveEntity.Comp.FlowRate = 0;
                DirtyFields(valveEntity,
                    valveEntity.Comp,
                    MetaData(valveEntity),
                    nameof(valveEntity.Comp.FlowRate),
                    nameof(valveEntity.Comp.Enabled));
                return;
            }

            DirtyField(valveEntity!, nameof(valveEntity.Comp.Enabled));
        }
    }
}
