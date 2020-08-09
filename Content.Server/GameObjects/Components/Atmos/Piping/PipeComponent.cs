using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class PipeComponent : Component, IGasMixtureHolder
    {
        public override string Name => "Pipe";

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture ContainedGas => _needsPipeNet ? Air : _pipeNet.Air;

        /// <summary>
        ///     Stores gas in this pipe when disconnected <see cref="IPipeNet"/>.
        ///     Only for usage by <see cref="IPipeNet"/>s.
        /// </summary>
        [ViewVariables]
        public GasMixture Air { get; set; }

        [ViewVariables]
        public float Volume { get; private set; }

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Volume, "volume", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
            Air = new GasMixture(Volume);

            //debug way for some gas to start in pipes
            Air.AdjustMoles(0, 1);
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

        public bool AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);
            return true;
        }
    }
}
