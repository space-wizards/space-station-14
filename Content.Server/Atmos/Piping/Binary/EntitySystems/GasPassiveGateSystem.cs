using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasPassiveGateSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPassiveGateComponent, AtmosDeviceUpdateEvent>(OnPassiveGateUpdated);
        }

        private void OnPassiveGateUpdated(EntityUid uid, GasPassiveGateComponent gate, AtmosDeviceUpdateEvent args)
        {
            if (!gate.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(gate.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(gate.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;
            var inputStartingPressure = inlet.Air.Pressure;

            if (outputStartingPressure >= MathF.Min(gate.TargetPressure, inputStartingPressure - gate.FrictionPressureDifference))
                return; // No need to pump gas, target reached or input pressure too low.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = MathF.Min(gate.TargetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure)/2);
                // We can't have a pressure delta that would cause outlet pressure > inlet pressure.

                var transferMoles = pressureDelta * outlet.Air.Volume / (inlet.Air.Temperature * Atmospherics.R);

                // Actually transfer the gas.
                _atmosphereSystem.Merge(outlet.Air, inlet.Air.Remove(transferMoles));
            }
        }
    }
}
