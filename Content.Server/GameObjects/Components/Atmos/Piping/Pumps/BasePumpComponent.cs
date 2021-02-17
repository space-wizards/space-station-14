#nullable enable
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Pumps
{
    /// <summary>
    ///     Transfer gas from one <see cref="PipeNode"/> to another.
    /// </summary>
    public abstract class BasePumpComponent : Component, IActivate
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
        ///     Needs to be same <see cref="PipeDirection"/> as that of a <see cref="PipeNode"/> on this entity.
        /// </summary>
        [ViewVariables]
        private PipeDirection _initialInletDirection;

        /// <summary>
        ///     Needs to be same <see cref="PipeDirection"/> as that of a <see cref="PipeNode"/> on this entity.
        /// </summary>
        [ViewVariables]
        private PipeDirection _initialOutletDirection;

        [ViewVariables]
        private PipeNode? _inletPipe;

        [ViewVariables]
        private PipeNode? _outletPipe;

        private AppearanceComponent? _appearance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _initialInletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _initialOutletDirection, "outletDirection", PipeDirection.None);
            serializer.DataField(ref _pumpEnabled, "pumpEnabled", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PipeNetDeviceComponent>();
            SetPipes();
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
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
            if (!PumpEnabled)
                return;

            if (_inletPipe == null || _outletPipe == null)
                return;

            PumpGas(_inletPipe.Air, _outletPipe.Air);
        }

        protected abstract void PumpGas(GasMixture inletGas, GasMixture outletGas);

        private void UpdateAppearance()
        {
            if (_inletPipe == null || _outletPipe == null) return;
            _appearance?.SetData(PumpVisuals.VisualState, new PumpVisualState(_initialInletDirection, _initialOutletDirection, PumpEnabled));
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            PumpEnabled = !PumpEnabled;
        }

        private void SetPipes()
        {
            _inletPipe = null;
            _outletPipe = null;

            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Error($"{nameof(BasePumpComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            var pipeNodes = container.Nodes.OfType<PipeNode>();
            _inletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _initialInletDirection).FirstOrDefault();
            _outletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _initialOutletDirection).FirstOrDefault();
            if (_inletPipe == null || _outletPipe == null)
            {
                Logger.Error($"{nameof(BasePumpComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
