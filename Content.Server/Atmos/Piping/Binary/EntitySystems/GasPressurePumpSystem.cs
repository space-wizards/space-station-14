using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed class GasPressurePumpSystem : SharedGasPressurePumpSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
    }

    private void OnPumpUpdated(Entity<GasPressurePumpComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!ent.Comp.Enabled
            || !_power.IsPowered(ent)
            || !_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.InletName, ent.Comp.OutletName, out PipeNode? inlet, out PipeNode? outlet))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        var outputStartingPressure = outlet.Air.Pressure;

        if (outputStartingPressure >= ent.Comp.TargetPressure)
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return; // No need to pump gas if target has been reached.
        }

        if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
        {
            // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
            var pressureDelta = ent.Comp.TargetPressure - outputStartingPressure;
            var transferMoles = (pressureDelta * outlet.Air.Volume) / (inlet.Air.Temperature * Atmospherics.R);

            var removed = inlet.Air.Remove(transferMoles);
            _atmosphereSystem.Merge(outlet.Air, removed);
            _ambientSoundSystem.SetAmbience(ent, removed.TotalMoles > 0f);
        }
    }
}
