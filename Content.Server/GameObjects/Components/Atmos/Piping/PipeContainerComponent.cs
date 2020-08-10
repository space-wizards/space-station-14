using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class PipeContainerComponent : Component
    {
        public override string Name => "PipeContainer";

        [ViewVariables]
        public IReadOnlyList<Pipe> Pipes => _pipes;
        private List<Pipe> _pipes = new List<Pipe>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipes, "pipes", new List<Pipe>());
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var pipe in _pipes)
            {
                pipe.Initialize();
            }
        }
    }
    public class Pipe : IGasMixtureHolder, IExposeData
    {
        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => _needsPipeNet ? LocalAir : _pipeNet.Air;
            set
            {
                if (_needsPipeNet)
                    LocalAir = value;
                else
                    _pipeNet.Air = value;
            }
        }

        /// <summary>
        ///     Stores gas in this pipe when disconnected from a <see cref="IPipeNet"/>.
        ///     Only for usage by <see cref="IPipeNet"/>s.
        /// </summary>
        [ViewVariables]
        public GasMixture LocalAir { get; set; }

        [ViewVariables]
        public float Volume { get; private set; }

        [ViewVariables]
        public PipeDirection PipeDirection { get; private set; }

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => Volume, "volume", 10);
            serializer.DataField(this, x => PipeDirection, "pipeDirection", PipeDirection.None);
        }

        public void Initialize()
        {
            LocalAir = new GasMixture(Volume);

            //debug way for some gas to start in pipes
            LocalAir.AdjustMoles(0, 1000);
            LocalAir.Temperature = 500;
        }

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            _pipeNet = pipeNet;
            _needsPipeNet = false;
        }

        public void ClearPipeNet()
        {
            _pipeNet = PipeNet.NullNet;
            _needsPipeNet = true;
        }

        public bool AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);
            return true;
        }
    }
}
