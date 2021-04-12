using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Binary
{
    [RegisterComponent]
    public class GasPumpComponent : Component, IAtmosProcess
    {
        public override string Name => "GasPump";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

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

            if (MathHelper.CloseTo(_targetPressure, outputStartingPressure))
                return; // No need to pump gas if target has been reached.

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = _targetPressure - outputStartingPressure;
                var transferMoles = pressureDelta * outlet.Air.Volume / inlet.Air.Temperature * Atmospherics.R;

                var removed = inlet.Air.Remove(transferMoles);
                outlet.Air.Merge(removed);
            }
        }
    }
}
