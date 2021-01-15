#nullable enable
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Filters
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
        private Gas _gasToFilter;

        [ViewVariables(VVAccess.ReadWrite)]
        public int VolumeFilterRate
        {
            get => _volumeFilterRate;
            set => _volumeFilterRate = Math.Clamp(value, 0, MaxVolumeFilterRate);
        }
        private int _volumeFilterRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxVolumeFilterRate
        {
            get => _maxVolumeFilterRate;
            set => Math.Max(value, 0);
        }
        private int _maxVolumeFilterRate;

        [ViewVariables]
        private PipeDirection _initialInletDirection;

        /// <summary>
        ///     The direction the filtered-out gas goes.
        /// </summary>
        [ViewVariables]
        private PipeDirection _initialFilterOutletDirection;

        /// <summary>
        ///     The direction the rest of the gas goes.
        /// </summary>
        [ViewVariables]
        private PipeDirection _initialOutletDirection;

        [ViewVariables]
        private PipeNode? _inletPipe;

        [ViewVariables]
        private PipeNode? _filterOutletPipe;

        [ViewVariables]
        private PipeNode? _outletPipe;

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance = default;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _volumeFilterRate, "startingVolumePumpRate", 0);
            serializer.DataField(ref _maxVolumeFilterRate, "maxVolumePumpRate", 100);
            serializer.DataField(ref _gasToFilter, "gasToFilter", Gas.Phoron);
            serializer.DataField(ref _initialInletDirection, "inletDirection", PipeDirection.None);
            serializer.DataField(ref _initialFilterOutletDirection, "filterOutletDirection", PipeDirection.None);
            serializer.DataField(ref _initialOutletDirection, "outletDirection", PipeDirection.None);
        }

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
                Logger.Error($"{typeof(GasFilterComponent)} on {Owner?.Prototype?.ID}, Uid {Owner?.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }
    }
}
