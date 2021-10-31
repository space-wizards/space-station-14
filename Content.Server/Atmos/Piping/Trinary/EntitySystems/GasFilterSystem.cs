using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems
{
    [UsedImplicitly]
    public class GasFilterSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, AtmosDeviceUpdateEvent args)
        {
            var appearance = filter.Owner.GetComponentOrNull<AppearanceComponent>();

            if (!filter.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !EntityManager.TryGetComponent(uid, out AtmosDeviceComponent? device)
            || !nodeContainer.TryGetNode(filter.InletName, out PipeNode? inletNode)
            || !nodeContainer.TryGetNode(filter.FilterName, out PipeNode? filterNode)
            || !nodeContainer.TryGetNode(filter.OutletName, out PipeNode? outletNode)
            || outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure) // No need to transfer if target is full.
            {
                appearance?.SetData(FilterVisuals.Enabled, false);
                return;
            }

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferRatio = (float)(filter.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds) / inletNode.Air.Volume;

            if (transferRatio <= 0)
            {
                appearance?.SetData(FilterVisuals.Enabled, false);
                return;
            }

            var removed = inletNode.Air.RemoveRatio(transferRatio);

            if (filter.FilteredGas.HasValue)
            {
                appearance?.SetData(FilterVisuals.Enabled, true);

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
