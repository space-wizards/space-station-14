using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasVolumePumpSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVolumePumpComponent, AtmosDeviceUpdateEvent>(OnVolumePumpUpdated);
        }

        private void OnVolumePumpUpdated(EntityUid uid, GasVolumePumpComponent pump, AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!ComponentManager.TryGetComponent(uid, out AtmosDeviceComponent? device))
                return;

            if (!nodeContainer.TryGetNode(pump.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(pump.OutletName, out PipeNode? outlet))
                return;

            var inputStartingPressure = inlet.Air.Pressure;
            var outputStartingPressure = outlet.Air.Pressure;

            // Pump mechanism won't do anything if the pressure is too high/too low unless you overclock it.
            if ((inputStartingPressure < pump.LowerThreshold) || (outputStartingPressure > pump.HigherThreshold) && !pump.Overclocked)
                return;

            // Overclocked pumps can only force gas a certain amount.
            if ((outputStartingPressure - inputStartingPressure > pump.OverclockThreshold) && pump.Overclocked)
                return;

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferRatio = (float)(pump.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds) / inlet.Air.Volume;

            var removed = inlet.Air.RemoveRatio(transferRatio);

            // Some of the gas from the mixture leaks when overclocked.
            if (pump.Overclocked)
            {
                var tile = args.Atmosphere.GetTile(pump.Owner.Transform.Coordinates);

                if (tile != null)
                {
                    var leaked = removed.RemoveRatio(pump.LeakRatio);
                    tile.AssumeAir(leaked);
                }
            }

            outlet.AssumeAir(removed);
        }
    }
}
