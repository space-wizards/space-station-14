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

namespace Content.Server.GameObjects.Components.Atmos.Piping.Trinary
{
    [RegisterComponent]
    public class GasFilterComponent : Component, IAtmosProcess
    {
        public override string Name => "GasFilter";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        private string _inletName = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("filter")]
        private string _filterName = "filter";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        private string _outletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        private float _transferRate = Atmospherics.MaxTransferRate;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gas? FilteredGas { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Min(value, Atmospherics.MaxTransferRate);
        }

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!_enabled)
                return;

            if (!Owner.TryGetComponent(out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(_inletName, out PipeNode? inletNode)
                || !nodeContainer.TryGetNode(_filterName, out PipeNode? filterNode)
                || !nodeContainer.TryGetNode(_outletName, out PipeNode? outletNode))
                return;

            if (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure)
                return; // No need to transfer if target is full.

            // We take time into account here, transfer rates are L/s, after all.
            var transferRatio = _transferRate * time / inletNode.Air.Volume;

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
