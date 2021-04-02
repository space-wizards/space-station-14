#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Placeholder component for adjusting the temperature of gas in pipes.
    /// </summary>
    [RegisterComponent]
    public class PipeHeaterComponent : Component
    {
        public override string Name => "PipeHeater";

        [ViewVariables]
        private PipeNode? _heaterPipe;

        [ViewVariables(VVAccess.ReadWrite)]
        private float TargetTemperature { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            SetPipe();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PipeNetUpdateMessage:
                    Update();
                    break;
            }
        }

        public void Update()
        {
            if (_heaterPipe == null)
                return;

            _heaterPipe.Air.Temperature = TargetTemperature;
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
    }
}
