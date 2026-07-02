using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasFilterSystem : SharedGasFilterSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasFilterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
        SubscribeLocalEvent<GasFilterComponent, AtmosDeviceDisabledEvent>(OnFilterLeaveAtmosphere);
        SubscribeLocalEvent<GasFilterComponent, GasAnalyzerScanEvent>(OnFilterAnalyzed);
    }

    private void OnInit(Entity<GasFilterComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnFilterUpdated(Entity<GasFilterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!ent.Comp.Enabled
            || !_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.InletName, ent.Comp.FilterName, ent.Comp.OutletName, out PipeNode? inletNode, out PipeNode? filterNode, out PipeNode? outletNode)
            || (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure && filterNode.Air.Pressure >= Atmospherics.MaxOutputPressure)) // No need to transfer if targets are full.
        {
            _ambientSoundSystem.SetAmbience(ent.Owner, false);
            return;
        }

        // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
        var transferVol = ent.Comp.TransferRate * _atmosphereSystem.PumpSpeedup() * args.dt;

        if (transferVol <= 0)
        {
            _ambientSoundSystem.SetAmbience(ent.Owner, false);
            return;
        }

        var removed = inletNode.Air.RemoveVolume(transferVol);

        if (ent.Comp.FilteredGas.HasValue)
        {
            // Make sure we don't pump over the pressure limit.
            var limitMolesFilter =
                AtmosphereSystem.MolesToMaxPressure(removed, filterNode.Air, Atmospherics.MaxOutputPressure);

            var availableMoles = removed.GetMoles(ent.Comp.FilteredGas.Value);
            var filteredMoles = Math.Max(Math.Min(limitMolesFilter, availableMoles), 0);
            var filteredGasMixture = new GasMixture { Temperature = removed.Temperature };

            filteredGasMixture.SetMoles(ent.Comp.FilteredGas.Value, filteredMoles);
            removed.AdjustMoles(ent.Comp.FilteredGas.Value, -filteredMoles);

            _atmosphereSystem.Merge(filterNode.Air, filteredGasMixture);

            _ambientSoundSystem.SetAmbience(ent.Owner, filteredMoles > 0f);
        }

        // Fraction of `removed` that can be sent to outlet without exceeding max pressure.
        var limitRatioOutlet =
            AtmosphereSystem.FractionToMaxPressure(removed, outletNode.Air, Atmospherics.MaxOutputPressure);

        // This might end up negative, but such cases are handled correctly by the `RemoveRatio` method
        var passthrough = removed.RemoveRatio(limitRatioOutlet);

        _atmosphereSystem.Merge(outletNode.Air, passthrough);
        _atmosphereSystem.Merge(inletNode.Air, removed);
    }

    private void OnFilterLeaveAtmosphere(Entity<GasFilterComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);

        UpdateAppearance(ent);
        _ambientSoundSystem.SetAmbience(ent.Owner, false);

        _ui.CloseUi(ent.Owner, GasFilterUiKey.Key);
    }

    /// <summary>
    /// Returns the gas mixture for the gas analyzer
    /// </summary>
    private void OnFilterAnalyzed(Entity<GasFilterComponent> ent, ref GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();

        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
        {
            var inletAirLocal = inlet.Air.Clone();
            inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
            inletAirLocal.Volume = inlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
        }
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.FilterName, out PipeNode? filterNode) && filterNode.Air.Volume != 0f)
        {
            var filterNodeAirLocal = filterNode.Air.Clone();
            filterNodeAirLocal.Multiply(filterNode.Volume / filterNode.Air.Volume);
            filterNodeAirLocal.Volume = filterNode.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-filter"), filterNodeAirLocal));
        }
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.OutletName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
        {
            var outletAirLocal = outlet.Air.Clone();
            outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
            outletAirLocal.Volume = outlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
        }

        args.DeviceFlipped = inlet != null && filterNode != null && inlet.CurrentPipeDirection.ToDirection() == filterNode.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
    }
}
