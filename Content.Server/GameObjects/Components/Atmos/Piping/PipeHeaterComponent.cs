#nullable enable
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Placeholder component for adjusting the temperature of gas in pipes.
    /// </summary>
    [RegisterComponent]
    public class PipeHeaterComponent : Component, IAtmosProcess
    {
        public override string Name => "PipeHeater";

        [ViewVariables]
        private PipeNode? _heaterPipe;

        [ViewVariables(VVAccess.ReadWrite)]
        private float TargetTemperature { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<AtmosDeviceComponent>();
            SetPipe();
        }

        private void SetPipe()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Warning($"{nameof(PipeHeaterComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _heaterPipe = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_heaterPipe == null)
            {
                Logger.Warning($"{nameof(PipeHeaterComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }

        public void ProcessAtmos(IGridAtmosphereComponent atmosphere)
        {
            if (_heaterPipe == null)
                return;

            _heaterPipe.Air.Temperature = TargetTemperature;
        }
    }
}
