using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Pumps
{
    /// <summary>
    ///     Transfer gas from one <see cref="PipeNode"/> to another.
    /// </summary>
    public abstract class BasePumpComponent : PipeNetDeviceComponent
    {
        /// <summary>
        ///     If the pump is currently pumping.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool PumpEnabled
        {
            get => _pumpEnabled;
            set
            {
                _pumpEnabled = value;
                UpdateAppearance();
            }
        }
        private bool _pumpEnabled;

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

        private AppearanceComponent _appearance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _inletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _outletDirection, "outletDirection", PipeDirection.None);
            serializer.DataField(ref _pumpEnabled, "pumpEnabled", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdatePipes();
            Owner.EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public override void Update()
        {
            if (!PumpEnabled)
                return;

            PumpGas(_inletPipe.Air, _outletPipe.Air);
        }

        protected abstract void PumpGas(GasMixture inletGas, GasMixture outletGas);

        private void RotateEvent(RotateEvent ev)
        {
            if (ev.Sender != Owner || ev.NewRotation == ev.OldRotation)
                return;

            var diff = ev.NewRotation - ev.OldRotation;
            _inletDirection = _inletDirection.RotatePipeDirection(diff);
            _outletDirection = _outletDirection.RotatePipeDirection(diff);
            UpdatePipes();
        }

        private void UpdateAppearance()
        {
            if (_inletPipe == null || _outletPipe == null) return;
            _appearance?.SetData(PumpVisuals.VisualState, new PumpVisualState(_inletDirection, _outletDirection, _inletPipe.ConduitLayer, _outletPipe.ConduitLayer, PumpEnabled));
        }

        private void UpdatePipes()
        {
            _inletPipe = null;
            _outletPipe = null;

            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BasePumpComponent)} on entity {Owner.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            var pipeNodes = container.Nodes.OfType<PipeNode>();
            _inletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _inletDirection).FirstOrDefault();
            _outletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _outletDirection).FirstOrDefault();
            if (_inletPipe == null | _outletPipe == null)
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BasePumpComponent)} on entity {Owner.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
