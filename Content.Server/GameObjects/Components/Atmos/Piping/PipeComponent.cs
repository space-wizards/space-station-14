using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    public class PipeComponent : Component
    {
        public override string Name => "Pipe";

        /// <summary>
        ///     To check what gases are in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture ContainedGas => _needsPipeNet ? LocalGas : _pipeNet.ContainedGas;

        /// <summary>
        ///     Stores gas in this pipe when not in an <see cref="IPipeNet"/>.
        ///     Only for usage by <see cref="IPipeNet"/>s.
        /// </summary>
        [ViewVariables]
        public GasMixture LocalGas { get; set; }

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            _pipeNet = pipeNet;
            _needsPipeNet = false;
        }

        public void ClearPipeNet()
        {
            _pipeNet = NodeContainer.NodeGroups.PipeNet.NullNet;
            _needsPipeNet = true;
        }
    }
}
