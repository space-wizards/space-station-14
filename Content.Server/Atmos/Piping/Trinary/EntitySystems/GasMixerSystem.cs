using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems;

[UsedImplicitly]
public sealed class GasMixerSystem : SharedGasMixerSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMixerComponent, AtmosDeviceUpdateEvent>(OnMixerUpdated);
        SubscribeLocalEvent<GasMixerComponent, GasAnalyzerScanEvent>(OnMixerAnalyzed);
    }

    private void OnMixerUpdated(Entity<GasMixerComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        // TODO ATMOS: Cache total moles since it's expensive.

        if (!ent.Comp.Enabled
            || !_power.IsPowered(ent)
            || !_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.InletOneName, ent.Comp.InletTwoName, ent.Comp.OutletName, out PipeNode? inletOne, out PipeNode? inletTwo, out PipeNode? outlet))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        var outputStartingPressure = outlet.Air.Pressure;

        if (outputStartingPressure >= ent.Comp.TargetPressure)
            return; // Target reached, no need to mix.

        var generalTransfer = (ent.Comp.TargetPressure - outputStartingPressure) * outlet.Air.Volume / Atmospherics.R;

        var transferMolesOne = inletOne.Air.Temperature > 0 ? ent.Comp.InletOneConcentration * generalTransfer / inletOne.Air.Temperature : 0f;
        var transferMolesTwo = inletTwo.Air.Temperature > 0 ? ent.Comp.InletTwoConcentration * generalTransfer / inletTwo.Air.Temperature : 0f;

        if (ent.Comp.InletTwoConcentration <= 0f)
        {
            if (inletOne.Air.Temperature <= 0f)
                return;

            transferMolesOne = MathF.Min(transferMolesOne, inletOne.Air.TotalMoles);
            transferMolesTwo = 0f;
        }

        else if (ent.Comp.InletOneConcentration <= 0)
        {
            if (inletTwo.Air.Temperature <= 0f)
                return;

            transferMolesOne = 0f;
            transferMolesTwo = MathF.Min(transferMolesTwo, inletTwo.Air.TotalMoles);
        }
        else
        {
            if (inletOne.Air.Temperature <= 0f || inletTwo.Air.Temperature <= 0f)
                return;

            if (transferMolesOne <= 0 || transferMolesTwo <= 0)
            {
                _ambientSoundSystem.SetAmbience(ent, false);
                return;
            }

            if (inletOne.Air.TotalMoles < transferMolesOne || inletTwo.Air.TotalMoles < transferMolesTwo)
            {
                var ratio = MathF.Min(inletOne.Air.TotalMoles / transferMolesOne, inletTwo.Air.TotalMoles / transferMolesTwo);
                transferMolesOne *= ratio;
                transferMolesTwo *= ratio;
            }
        }

        // Actually transfer the gas now.
        var transferred = false;

        if (transferMolesOne > 0f)
        {
            transferred = true;
            var removed = inletOne.Air.Remove(transferMolesOne);
            _atmosphereSystem.Merge(outlet.Air, removed);
        }

        if (transferMolesTwo > 0f)
        {
            transferred = true;
            var removed = inletTwo.Air.Remove(transferMolesTwo);
            _atmosphereSystem.Merge(outlet.Air, removed);
        }

        if (transferred)
            _ambientSoundSystem.SetAmbience(ent, true);
    }

    /// <summary>
    /// Returns the gas mixture for the gas analyzer
    /// </summary>
    private void OnMixerAnalyzed(Entity<GasMixerComponent> ent, ref GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= [];

        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletOneName, out PipeNode? inletOne) && inletOne.Air.Volume != 0f)
        {
            var inletOneAirLocal = inletOne.Air.Clone();
            inletOneAirLocal.Multiply(inletOne.Volume / inletOne.Air.Volume);
            inletOneAirLocal.Volume = inletOne.Volume;
            args.GasMixtures.Add(($"{inletOne.CurrentPipeDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletOneAirLocal));
        }
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletTwoName, out PipeNode? inletTwo) && inletTwo.Air.Volume != 0f)
        {
            var inletTwoAirLocal = inletTwo.Air.Clone();
            inletTwoAirLocal.Multiply(inletTwo.Volume / inletTwo.Air.Volume);
            inletTwoAirLocal.Volume = inletTwo.Volume;
            args.GasMixtures.Add(($"{inletTwo.CurrentPipeDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletTwoAirLocal));
        }
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.OutletName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
        {
            var outletAirLocal = outlet.Air.Clone();
            outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
            outletAirLocal.Volume = outlet.Volume;
            args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
        }

        args.DeviceFlipped = inletOne != null && inletTwo != null && inletOne.CurrentPipeDirection.ToDirection() == inletTwo.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
    }
}
