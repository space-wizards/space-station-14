#nullable enable
using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Generates gas in the attached pipe.
    /// </summary>
    [RegisterComponent]
    public class GasGeneratorComponent : Component
    {
        public override string Name => "GasGenerator";

        /// <summary>
        ///     If the generator is producing gas.
        /// </summary>
        [DataField("generatorEnabled")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool GeneratorEnabled { get; set; } = true;

        /// <summary>
        ///     What gas is being generated.
        /// </summary>
        [DataField("generatedGas")]
        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GeneratedGas { get; set; } = Gas.Oxygen;

        /// <summary>
        ///     Molar rate of gas generation.
        /// </summary>
        [DataField("gasGenerationRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float GasGenerationRate { get; set; } = 10;

        /// <summary>
        ///     The pipe pressure above which the generator stops producing gas.
        /// </summary>
        [DataField("generatorPressureCap")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float GeneratorPressureCap { get; set; } = 10;

        /// <summary>
        ///     The pipe to which generated gas is added.
        /// </summary>
        [ViewVariables]
        private PipeNode? Pipe { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            SetPipes();
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

        private void Update()
        {
            if (!GeneratorEnabled)
                return;

            if (Pipe == null || Pipe.Air.Pressure > GeneratorPressureCap)
                return;

            Pipe.Air.AdjustMoles(GeneratedGas, GasGenerationRate);
        }

        private void SetPipes()
        {
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Warning($"{nameof(GasGeneratorComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            Pipe = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (Pipe == null)
            {
                Logger.Warning($"{nameof(GasGeneratorComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
