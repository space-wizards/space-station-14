using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    public abstract class BasePumpComponent : Component
    {
        [ViewVariables]
        private PipeDirection _inletDirection;

        [ViewVariables]
        private PipeDirection _outletDirection;

        [ViewVariables]
        private Pipe _inletPipe;

        [ViewVariables]
        private Pipe _outletPipe;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _inletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _outletDirection, "outletDirection", PipeDirection.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            var pipeContainer = Owner.GetComponent<PipeContainerComponent>();
            _inletPipe = pipeContainer.Pipes.Where(pipe => pipe.PipeDirection == _inletDirection).First();
            _outletPipe = pipeContainer.Pipes.Where(pipe => pipe.PipeDirection == _outletDirection).First();
        }

        public void Update(float frameTime)
        {
            PumpGas(_inletPipe.Air, _outletPipe.Air, frameTime);
        }

        protected abstract void PumpGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
