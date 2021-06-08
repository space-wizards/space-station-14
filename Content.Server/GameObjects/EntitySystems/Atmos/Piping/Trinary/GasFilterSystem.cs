using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Trinary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Trinary
{
    [UsedImplicitly]
    public class GasFilterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, AtmosDeviceUpdateEvent args)
        {
            if (!filter.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(filter.InletName, out PipeNode? inletNode)
                || !nodeContainer.TryGetNode(filter.FilterName, out PipeNode? filterNode)
                || !nodeContainer.TryGetNode(filter.OutletName, out PipeNode? outletNode))
                return;

            if (outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure)
                return; // No need to transfer if target is full.

            // SUS: Maybe this should take time into account, transfer rate is L/s...
            var transferRatio = filter.TransferRate / inletNode.Air.Volume;

            if (transferRatio <= 0)
                return;

            var removed = inletNode.Air.RemoveRatio(transferRatio);

            if (filter.FilteredGas.HasValue)
            {
                var filteredOut = new GasMixture() {Temperature = removed.Temperature};

                filteredOut.SetMoles(filter.FilteredGas.Value, removed.GetMoles(filter.FilteredGas.Value));
                removed.SetMoles(filter.FilteredGas.Value, 0f);

                var target = filterNode.Air.Pressure < Atmospherics.MaxOutputPressure ? filterNode : inletNode;
                target.AssumeAir(filteredOut);
            }

            outletNode.AssumeAir(removed);
        }
    }
}
