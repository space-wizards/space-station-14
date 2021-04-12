using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasOutletInjectorComponent : Component, IAtmosProcess
    {
        public override string Name => "GasOutletInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _injecting = false;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _volumeRate = 50f;

        [DataField("inlet")]
        private string _inletName = "pipe";

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            _injecting = false;

            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inlet))
                return;

            var environment = atmosphere.GetTile(Owner.Transform.Coordinates)!;

            if (environment.Air == null)
                return;

            if (inlet.Air.Temperature > 0)
            {
                var transferMoles = inlet.Air.Pressure * _volumeRate / (inlet.Air.Temperature * Atmospherics.R);

                var removed = inlet.Air.Remove(transferMoles);

                environment.AssumeAir(removed);
                environment.Invalidate();
            }
        }

        // TODO ATMOS: Inject method.
    }
}
