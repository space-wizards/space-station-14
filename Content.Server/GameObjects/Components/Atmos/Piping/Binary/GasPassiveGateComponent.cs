using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Binary
{
    [RegisterComponent]
    public class GasPassiveGateComponent : Component, IAtmosProcess
    {
        public override string Name => "GasPassiveGate";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        /// <summary>
        ///     This is the minimum difference needed to overcome the friction in the mechanism.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("frictionDifference")]
        private float _frictionPressureDifference = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        private string _inletName = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        private float _targetPressure = Atmospherics.OneAtmosphere;

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(_outletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;
            var inputStartingPressure = inlet.Air.Pressure;

            if (outputStartingPressure >= MathF.Min(_targetPressure, inputStartingPressure - _frictionPressureDifference))
                return; // No need to pump gas, target reached or input pressure too low.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = MathF.Min(_targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure)/2);
                // We can't have a pressure delta that would cause outlet pressure > inlet pressure.

                var transferMoles = pressureDelta * outlet.Air.Volume / (inlet.Air.Temperature * Atmospherics.R);

                // Actually transfer the gas.
                outlet.Air.Merge(inlet.Air.Remove(transferMoles));
            }
        }
    }
}
