#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasFilterComponent : Component
    {
        public override string Name => "GasFilter";

        /// <summary>
        ///     If the filter is currently filtering.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool FilterEnabled
        {
            get => _filterEnabled;
            set
            {
                _filterEnabled = value;
                UpdateAppearance();
            }
        }
        private bool _filterEnabled;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GasToFilter
        {
            get => _gasToFilter;
            set
            {
                _gasToFilter = value;
                UpdateAppearance();
            }
        }

        [DataField("gasToFilter")] private Gas _gasToFilter = Gas.Plasma;

        [ViewVariables(VVAccess.ReadWrite)]
        public int VolumeFilterRate
        {
            get => _volumeFilterRate;
            set => _volumeFilterRate = Math.Clamp(value, 0, MaxVolumeFilterRate);
        }

        [DataField("startingVolumePumpRate")]
        private int _volumeFilterRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxVolumeFilterRate
        {
            get => _maxVolumeFilterRate;
            set => Math.Max(value, 0);
        }

        [DataField("maxVolumePumpRate")] private int _maxVolumeFilterRate = 100;

        [DataField("inletDirection")] [ViewVariables]
        private PipeDirection _initialInletDirection = PipeDirection.None;

        /// <summary>
        ///     The direction the filtered-out gas goes.
        /// </summary>
        [DataField("filterOutletDirection")] [ViewVariables]
        private PipeDirection _initialFilterOutletDirection = PipeDirection.None;

        /// <summary>
        ///     The direction the rest of the gas goes.
        /// </summary>
        [DataField("outletDirection")] [ViewVariables]
        private PipeDirection _initialOutletDirection = PipeDirection.None;

        [ViewVariables]
        private PipeNode? _inletPipe;

        [ViewVariables]
        private PipeNode? _filterOutletPipe;

        [ViewVariables]
        private PipeNode? _outletPipe;

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance = default;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PipeNetDeviceComponent>();
            SetPipes();
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
            if (!FilterEnabled)
                return;

            if (_inletPipe == null || _inletPipe.Air == null ||
                _filterOutletPipe == null || _filterOutletPipe.Air == null ||
                _outletPipe == null || _outletPipe.Air == null)
                return;

            FilterGas(_inletPipe.Air, _filterOutletPipe.Air, _outletPipe.Air);
        }

        private void FilterGas(GasMixture inletGas, GasMixture filterOutletGas, GasMixture outletGas)
        {
            var volumeRatio = Math.Clamp(VolumeFilterRate / inletGas.Volume, 0, 1);
            var gas = inletGas.RemoveRatio(volumeRatio);

            var molesToSeperate = gas.GetMoles(GasToFilter);
            gas.SetMoles(GasToFilter, 0);
            filterOutletGas.AdjustMoles(GasToFilter, molesToSeperate);

            outletGas.Merge(gas);
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(FilterVisuals.VisualState, new FilterVisualState(FilterEnabled));
        }

        private void SetPipes()
        {
            _inletPipe = null;
            _filterOutletPipe = null;
            _outletPipe = null;

            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                Logger.Error($"{typeof(GasFilterComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }

            var pipeNodes = container.Nodes.OfType<PipeNode>();

            _inletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _initialInletDirection).FirstOrDefault();
            _filterOutletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _initialFilterOutletDirection).FirstOrDefault();
            _outletPipe = pipeNodes.Where(pipe => pipe.PipeDirection == _initialOutletDirection).FirstOrDefault();

            if (_inletPipe == null || _filterOutletPipe == null || _outletPipe == null)
            {
                Logger.Error($"{nameof(GasFilterComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
