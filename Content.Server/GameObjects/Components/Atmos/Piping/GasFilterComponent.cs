#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    public class GasFilterComponent : Component, IAtmosProcess
    {
        public override string Name => "GasFilter";

        private bool _enabled;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        private string _inlet = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("filter")]
        private string _filter = "filter";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outlet = "outlet";

        private float _transferRate = Atmospherics.MaxTransferRate;

        /// <summary>
        ///     If the filter is currently filtering.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateAppearance();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Gas? FilteredGas { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Min(value, Atmospherics.MaxTransferRate);
        }
        public override void Initialize()
        {
            base.Initialize();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if(Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(FilterVisuals.VisualState, new FilterVisualState(Enabled));
        }

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!Enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode<PipeNode>(_inlet, out var inletNode)
                || !nodeContainer.TryGetNode<PipeNode>(_filter, out var filterNode)
                || !nodeContainer.TryGetNode<PipeNode>(_outlet, out var outletNode))
                return;

            if (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure)
                return; // No need to transfer if target is full.

            // We take time into account here, transfer rates are L/s, after all.
            var transferRatio = _transferRate * time / inletNode.Volume;

            if (transferRatio <= 0)
                return;

            var removed = inletNode.Air.RemoveRatio(transferRatio);

            if (FilteredGas.HasValue)
            {
                var filteredOut = new GasMixture {Temperature = removed.Temperature};

                filteredOut.SetMoles(FilteredGas.Value, removed.GetMoles(FilteredGas.Value));
                removed.SetMoles(FilteredGas.Value, 0f);

                var target = filterNode.Air.Pressure < Atmospherics.MaxOutputPressure ? filterNode.Air : inletNode.Air;
                target.Merge(filteredOut);
            }

            outletNode.Air.Merge(removed);
        }
    }
}
