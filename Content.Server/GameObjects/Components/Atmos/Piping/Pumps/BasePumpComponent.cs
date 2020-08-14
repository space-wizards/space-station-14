using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Transfer gas from one <see cref="PipeNode"/> to another.
    /// </summary>
    public abstract class BasePumpComponent : UpdatedPipingComponent
    {
        /// <summary>
        ///     Needs to be same <see cref="PipeDirection"/> as that of a <see cref="Pipe"/> on this entity.
        /// </summary>
        [ViewVariables]
        private PipeDirection _inletDirection;

        /// <summary>
        ///     Needs to be same <see cref="PipeDirection"/> as that of a <see cref="Pipe"/> on this entity.
        /// </summary>
        [ViewVariables]
        private PipeDirection _outletDirection;

        [ViewVariables]
        private PipeNode _inletPipe;

        [ViewVariables]
        private PipeNode _outletPipe;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _inletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _outletDirection, "outletDirection", PipeDirection.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            var pipeNodes = Owner.GetComponent<NodeContainerComponent>()
                .Nodes
                .OfType<PipeNode>();
            _inletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _inletDirection).First();
            _outletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _outletDirection).First();
        }

        public override void Update(float frameTime)
        {
            PumpGas(_inletPipe.Air, _outletPipe.Air, frameTime);
        }

        protected abstract void PumpGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
