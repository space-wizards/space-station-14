using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Combines their <see cref="PowerNet"/> with those from adjacent compatible <see cref="BasePowerNetConnector"/>s.
    /// </summary>
    [RegisterComponent]
    public class PowerWireComponent : BasePowerNetConnector
    {
        public override string Name => "PowerWire";

        public override void Initialize()
        {
            base.Initialize();
            SpreadPowerNet();
        }

        /// <summary>
        ///     Causes this wire to try to merge nets with nearby <see cref="BasePowerNetConnector"/>s, or give
        ///     them a powernet if they do not have one.
        /// </summary>
        /// <param name="remakingNet">
        ///     If true, other wires who are given a powernet will also spread. Spreading reduces the number of
        ///     powernets that are created when a powernet is remaking itself with <see cref="PowerNet.RebuildPowerNet"/>
        /// </param>
        public void SpreadPowerNet(bool remakingNet = false)
        {
            if (PowerNet == null)
            {
                return;
            }

            var adjacent = Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal());
            var nearbyConnectors = adjacent.SelectMany(entity => entity.GetAllComponents())
                .OfType<BasePowerNetConnector>();
            var nearbyNets = nearbyConnectors.Select(connector => connector.PowerNet);

            if (remakingNet)
            {
                var nearbyWires = adjacent.Select(entity => entity.TryGetComponent<PowerWireComponent>(out var connector) ? connector : null)
                    .Where(connector => connector != null);

                foreach (var wire in nearbyWires)
                {
                    if (wire.TrySetPowerNetIfNeeded(PowerNet))
                    {
                        wire.SpreadPowerNet(remakingNet: true);
                    }
                }
            }

            foreach (var wireNet in nearbyNets)
            {
                wireNet.TryMergeToNet(PowerNet);
            }

            foreach (var connector in nearbyConnectors)
            {
                connector.TrySetPowerNetIfNeeded(PowerNet);
            }
        }

        /// <inheritdoc />
        protected override bool TryJoinPowerNet(PowerNet powerNet)
        {
            return powerNet.TryAddWire(this);
        }

        /// <inheritdoc />
        protected override void NotifyPowerNetOfLeaving()
        {
            PowerNet.RemoveWire(this);
        }

        protected override void OnSetVoltage()
        {
            base.OnSetVoltage();
            SpreadPowerNet();
        }
    }
}
