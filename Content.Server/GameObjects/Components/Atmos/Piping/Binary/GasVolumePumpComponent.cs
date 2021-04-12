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
    public class GasVolumePumpComponent : Component, IAtmosProcess
    {
        public override string Name => "GasVolumePump";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _overclocked = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        private string _inletName = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        private float _transferRate = Atmospherics.MaxTransferRate;

        [DataField("leakRatio")]
        private float _leakRatio = 0.1f;

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(_outletName, out PipeNode? outlet))
                return;

            var inputStartingPressure = inlet.Air.Pressure;
            var outputStartingPressure = outlet.Air.Pressure;

            // Pump mechanism won't do anything if the pressure is too high/too low unless you overclock it.
            if ((inputStartingPressure < 0.01f) || (outputStartingPressure > 9000) && !_overclocked)
                return;

            // Overclocked pumps can only force gas a certain amount.
            if ((outputStartingPressure - inputStartingPressure > 1000) && _overclocked)
                return;

            var transferRatio = _transferRate / inlet.Air.Volume;

            var removed = inlet.Air.RemoveRatio(transferRatio);

            // Some of the gas from the mixture leaks when overclocked.
            if (_overclocked)
            {
                var tile = atmosphere.GetTile(Owner.Transform.Coordinates);

                if (tile != null)
                {
                    var leaked = removed.RemoveRatio(_leakRatio);
                    tile.AssumeAir(leaked);
                }
            }

            outlet.Air.Merge(removed);
        }
    }
}
