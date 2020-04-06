using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Joins a <see cref="PowerNet"/> when created, allowing them to interact with the
    ///     power system.
    /// </summary>
    public abstract class BasePowerNetConnector : Component
    {
        /// <summary>
        ///     Connectors can only connect to the powernet of wires with the same voltage.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Voltage Voltage { get => _voltage; set => SetVoltage(value); }
        private Voltage _voltage;

        /// <summary>
        ///     The powernet this connector is currently a member of.
        /// </summary>
        [ViewVariables]
        public PowerNet PowerNet { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _voltage, "voltage", Voltage.High);
        }

        public override void Initialize()
        {
            base.Initialize();
            EnsureHasPowerNet();
        }

        public override void OnRemove()
        {
            LeavePowerNet();
            base.OnRemove();
        }

        /// <summary>
        ///     If this connector is not in a PowerNet, causes this connector to join a PowerNet
        ///     on an adjacent wire or make one for itself.
        /// </summary>
        /// <returns>Returns whether or not this connector needed a powernet.</returns>
        public bool EnsureHasPowerNet()
        {
            if (PowerNet != null)
                return false;

            //try to join an adjacent powernet
            var nearbyWireNets = Owner.GetComponent<SnapGridComponent>()
                .GetCardinalNeighborCells()
                .SelectMany(sgc => sgc.GetLocal())
                .Select(entity => entity.TryGetComponent<PowerWireComponent>(out var connector) ? connector : null)
                .Where(connector => connector != null)
                .Select(connector => connector.PowerNet)
                .Where(wireNet => wireNet != null);

            foreach (var wireNet in nearbyWireNets)
            {
                if (TrySetPowerNet(wireNet))
                    break;
            }
            //otherwise make own powernet
            if (PowerNet == null)
            {
                Debug.Assert(TrySetPowerNet(new PowerNet(this)));
            }
            return true;
        }

        /// <summary>
        ///     Sets this connector's powernet reference if it sucessfully joins the supplied
        ///     powernet.
        /// </summary>
        /// <returns>Returns whether or not this could join the supplied powernet.</returns>
        public bool TrySetPowerNet(PowerNet powerNet)
        {
            if (TryJoinPowerNet(powerNet))
            {
                LeavePowerNet();
                PowerNet = powerNet;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///     If this connector does not have a powernet, tries to set it to the provided
        ///     powernet.
        /// </summary>
        /// <returns>Returns whether or not this joined the supplied powernet.</returns>
        public bool TrySetPowerNetIfNeeded(PowerNet powerNet)
        {
            if (PowerNet != null)
            {
                return false;
            }
            else
            {
                return TrySetPowerNet(powerNet);
            }
        }

        /// <summary>
        ///     If this has a powernet, leave it and notify the powernet.
        /// </summary>
        public void LeavePowerNet()
        {
            if (PowerNet != null)
            {
                NotifyPowerNetOfLeaving();
                PowerNet = null;
            }
        }

        /// <summary>
        ///     Causes connector to try to join the provided powernet.
        /// </summary>
        /// <returns>
        ///     Returns whether or not this could sucessfully join the supplied powernet.
        /// </returns>
        protected abstract bool TryJoinPowerNet(PowerNet powerNet);

        /// <summary>
        ///     Notifes the connector's set powernet that it should remove this connector
        ///     from itself.
        /// </summary>
        protected abstract void NotifyPowerNetOfLeaving();

        protected virtual void OnSetVoltage() { }

        private void SetVoltage(Voltage voltage)
        {
            LeavePowerNet();
            _voltage = voltage;
            EnsureHasPowerNet();
            OnSetVoltage();
        }
    }
}
