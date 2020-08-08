using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class PipeComponent : Component
    {
        public override string Name => "Pipe";

        /// <summary>
        ///     The gases in this pipe.
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
        private float _volume;

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _volume, "volume", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
            LocalGas = new GasMixture(_volume);

            //debug
            LocalGas.AdjustMoles(0, 1);
        }

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
