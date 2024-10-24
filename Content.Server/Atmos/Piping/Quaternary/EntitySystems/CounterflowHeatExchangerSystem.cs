using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Quaternary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Quaternary.EntitySystems
{
    [UsedImplicitly]
    public sealed class CounterflowHeatExchangerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CounterflowHeatExchangerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
            SubscribeLocalEvent<CounterflowHeatExchangerComponent, GasAnalyzerScanEvent>(OnAnalyzed);
        }

        private void OnAtmosUpdate(EntityUid uid, CounterflowHeatExchangerComponent comp, ref AtmosDeviceUpdateEvent args)
        {
            if (!_nodeContainer.TryGetNodes(uid, comp.InletName, comp.InletSecondaryName, comp.OutletName, comp.OutletSecondaryName,
                out PipeNode? inlet, out PipeNode? inletSecondary, out PipeNode? outlet, out PipeNode? outletSecondary))
                return;

            var (airA, δpA) = GetAirTransfer(inlet.Air, outlet.Air);
            var (airB, δpB) = GetAirTransfer(inletSecondary.Air, outletSecondary.Air);

            var cA = _atmosphereSystem.GetHeatCapacity(airA, true);
            var cB = _atmosphereSystem.GetHeatCapacity(airB, true);

            if (airA.Pressure > 0 && airB.Pressure > 0)
            {
                bool hotA = airA.Temperature > airB.Temperature;
                var Th_in = hotA ? airA.Temperature : airB.Temperature;
                var Tc_in = hotA ? airB.Temperature : airA.Temperature;

                // Assume outlet temperatures equalize over time
                var Tf = (airA.Temperature * cA + airB.Temperature * cB) / (cA + cB);

                // Calculate LMTD
                var deltaT1 = Th_in - Tc_in;
                var deltaT2 = Tf - Tc_in;
                var LMTD = (deltaT1 - deltaT2) / MathF.Log(deltaT1 / deltaT2);

                // Heat transfer coefficient and area (defined in the component or calculated based on geometry)
                var U = comp.HeatTransferCoefficient;
                var A = comp.HeatExchangeArea;

                // Heat transfer rate
                var Q = U * A * LMTD;

                // Energy balance to calculate outlet temperatures
                var heatCapacityA = cA * airA.TotalMoles;
                var heatCapacityB = cB * airB.TotalMoles;

                var deltaTA = Q / heatCapacityA;
                var deltaTB = Q / heatCapacityB;

                if (hotA)
                {
                    airA.Temperature -= deltaTA;
                    airB.Temperature += deltaTB;
                }
                else
                {
                    airA.Temperature += deltaTA;
                    airB.Temperature -= deltaTB;
                }
            }

            // Update the air in the pipes
            _atmosphereSystem.Merge(outlet.Air, airA);
            _atmosphereSystem.Merge(outletSecondary.Air, airB);
        }

        private static (GasMixture, float δp) GetAirTransfer(GasMixture airInlet, GasMixture airOutlet)
        {
            var n1 = airInlet.TotalMoles;
            var n2 = airOutlet.TotalMoles;
            var p1 = airInlet.Pressure;
            var p2 = airOutlet.Pressure;
            var V1 = airInlet.Volume;
            var V2 = airOutlet.Volume;
            var T1 = airInlet.Temperature;
            var T2 = airOutlet.Temperature;

            var δp = p1 - p2;

            var denom = T1 * V2 + T2 * V1;

            if (δp > 0 && p1 > 0 && denom > 0)
            {
                var transferMoles = n1 - (n1 + n2) * T2 * V1 / denom;
                return (airInlet.Remove(transferMoles), δp);
            }

            return (new GasMixture(), δp);
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        ///
        private void OnAnalyzed(EntityUid uid, CounterflowHeatExchangerComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures ??= new List<(string, GasMixture?)>();

            if (_nodeContainer.TryGetNode(uid, component.InletName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
            {
                var inletAirLocal = inlet.Air.Clone();
                inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
                inletAirLocal.Volume = inlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.InletSecondaryName, out PipeNode? inletSecondary) && inletSecondary.Air.Volume != 0f)
            {
                var inletSecondaryAirLocal = inletSecondary.Air.Clone();
                inletSecondaryAirLocal.Multiply(inletSecondary.Volume / inletSecondary.Air.Volume);
                inletSecondaryAirLocal.Volume = inletSecondary.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet-secondary"), inletSecondaryAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.OutletName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
            {
                var outletAirLocal = outlet.Air.Clone();
                outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
                outletAirLocal.Volume = outlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.OutletSecondaryName, out PipeNode? outletSecondary) && outletSecondary.Air.Volume != 0f)
            {
                var outletSecondaryAirLocal = outletSecondary.Air.Clone();
                outletSecondaryAirLocal.Multiply(outletSecondary.Volume / outletSecondary.Air.Volume);
                outletSecondaryAirLocal.Volume = outletSecondary.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet-secondary"), outletSecondaryAirLocal));
            }
        }
    }
}
