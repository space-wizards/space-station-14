using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Maintains information about a collection of <see cref="BasePowerNetConnector"/>s and their subtypes.
    /// </summary>
    public class PowerNet
    {
        /// <summary>
        ///     True when a powernet is combining with another, supresses excess calculations.
        /// </summary>
        private bool _regenerating;

        /// <summary>
        ///     Only connectors of this powernet's voltage can join the powernet.
        /// </summary>
        [ViewVariables]
        private readonly Voltage _voltage;

        /// <summary>
        ///     The set of <see cref="BasePowerNetConnector"/>s that are in this power network.
        /// </summary>
        private readonly List<BasePowerNetConnector> _connectors = new List<BasePowerNetConnector>();

        /// <summary>
        /// The set of <see cref="PowerWireComponent"/>s that are in this power network.
        /// </summary>
        [ViewVariables]
        private readonly List<PowerWireComponent> _wires = new List<PowerWireComponent>();

        /// <summary>
        ///     The set of <see cref="PowerSupplierComponent"/>s that are in this power network.
        /// </summary>
        [ViewVariables]
        private readonly List<PowerSupplierComponent> _suppliers = new List<PowerSupplierComponent>();

        /// <summary>
        ///     The set of <see cref="PowerConsumerComponent"/>s that are in this power network.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<Priority, List<PowerConsumerComponent>> _consumersByPriority = new Dictionary<Priority, List<PowerConsumerComponent>>();

        /// <summary>
        ///     Total amount of electrical power (Watts) being supplied to this powernet by
        ///     <see cref="PowerSupplierComponent"/>s.
        /// </summary>
        [ViewVariables]
        private int _totalSupply = 0;

        /// <summary>
        ///     The amount of power being drawn by  <see cref="PowerConsumerComponent"/>s,
        ///     seperated by their <see cref="PowerConsumerComponent.Priority"/>.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<Priority, int> _drawByPriority = new Dictionary<Priority, int>();

        #region Debug Properties

        /// <summary>
        ///     UID of wire that first created this powernet.
        /// </summary>
        [ViewVariables]
        private readonly EntityUid _debugID;

        [ViewVariables]
        private int ConnectorCount => _connectors.Count;

        [ViewVariables]
        private int WireCount => _wires.Count;

        [ViewVariables]
        private int SupplierCount => _suppliers.Count;

        #endregion

        public PowerNet(BasePowerNetConnector sourceConnector)
        {
            _voltage = sourceConnector.Voltage;
            _debugID = sourceConnector.Owner.Uid;
            foreach (Priority priority in Enum.GetValues(typeof(Priority)))
            {
                _consumersByPriority.Add(priority, new List<PowerConsumerComponent>());
                _drawByPriority.Add(priority, 0);
            }
        }

        /// <summary>
        ///     Causes this powernet to give all its connectors to a supplied powernet, used to combine
        ///     multiple compatible powernets when a placed wire joins two previously seperate wire groups.
        /// </summary>
        public void TryMergeToNet(PowerNet powerNet)
        {
            if (powerNet == null || powerNet == this || powerNet._voltage != _voltage)
            {
                return;
            }
            
            if (_connectors.Count > powerNet._connectors.Count) //if we are bigger, they should give their connector to us (optimization)
            {
                powerNet.TryMergeToNet(this);
                return;
            }

            _regenerating = true;
            powerNet._regenerating = true;

            while (_connectors.Count > 0)
            {
                Debug.Assert(_connectors.First().TrySetPowerNet(powerNet));
            }
            powerNet._regenerating = false;
            powerNet.ReevaluatePower();
            //This should now be garbage collectable
        }

        public bool TryAddWire(PowerWireComponent wire)
        {
            if (wire.Voltage == _voltage)
            {
                _connectors.Add(wire);
                _wires.Add(wire);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveWire(PowerWireComponent wireToRemove)
        {
            Debug.Assert(_connectors.Remove(wireToRemove));
            Debug.Assert(_wires.Remove(wireToRemove));
            RebuildPowerNet();
        }

        public bool TryAddSupplier(PowerSupplierComponent supplier)
        {
            if (supplier.Voltage == _voltage)
            {
                _connectors.Add(supplier);
                _suppliers.Add(supplier);
                _totalSupply += supplier.SupplyRate;
                ReevaluatePower();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            Debug.Assert(_connectors.Remove(supplier));
            Debug.Assert(_suppliers.Remove(supplier));
            _totalSupply -= supplier.SupplyRate;
            ReevaluatePower();
        }

        public void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupply, int newSupply)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _totalSupply -= oldSupply;
            _totalSupply += newSupply;
            ReevaluatePower();
        }

        public bool TryAddConsumer(PowerConsumerComponent consumer)
        {
            if (consumer.Voltage == _voltage)
            {
                _connectors.Add(consumer);
                _consumersByPriority[consumer.Priority].Add(consumer);
                _drawByPriority[consumer.Priority] += consumer.DrawRate;
                ReevaluatePower();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            Debug.Assert(_connectors.Remove(consumer));
            Debug.Assert(_consumersByPriority[consumer.Priority].Remove(consumer));
            _drawByPriority[consumer.Priority] -= consumer.DrawRate;
            ReevaluatePower();
        }

        public void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDraw, int newDraw)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            _drawByPriority[consumer.Priority] -= oldDraw;
            _drawByPriority[consumer.Priority] += newDraw;
            ReevaluatePower();
        }

        public void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority)
        {
            Debug.Assert(_consumersByPriority[oldPriority].Remove(consumer));
            _drawByPriority[oldPriority] -= consumer.DrawRate;
            _consumersByPriority[newPriority].Add(consumer);
            _drawByPriority[newPriority] += consumer.DrawRate;
            ReevaluatePower();
        }

        /// <summary>
        ///     Removes all connectors, then ensures all wires have a net, and tell them to spread. Called when a wire is removed,
        ///     in case it has split a line of wires in two, so multiple new powernets are formed.
        /// </summary>
        private void RebuildPowerNet()
        {
            if (_regenerating)
            {
                return;
            }

            _regenerating = true;
            var wires = _wires.ToArray();

            while (_connectors.Count > 0)
            {
                _connectors.First().LeavePowerNet();
            }

            foreach (var wire in wires)
            {
                if (wire.EnsureHasPowerNet())
                {
                    wire.SpreadPowerNet(remakingNet: true);
                }
            }
            //This should now be garbage collectable
        }

        /// <summary>
        ///     Sets how much power every power consumer in the powernet is receiving.
        /// </summary>
        private void ReevaluatePower()
        {
            if (_regenerating)
            {
                return;
            }
            var remainingSupply = _totalSupply;
            foreach (Priority priority in Enum.GetValues(typeof(Priority)))
            {
                var categoryPowerDemand = _drawByPriority[priority];
                if (remainingSupply - categoryPowerDemand >= 0) //can fully power all in category
                {
                    remainingSupply -= categoryPowerDemand;
                    foreach (var consumer in _consumersByPriority[priority])
                    {
                        consumer.ReceivedPower = consumer.DrawRate;
                    }
                }
                else if (remainingSupply - categoryPowerDemand < 0) //cannot fully power all, split power
                {
                    var availiablePowerFraction = (float) remainingSupply / categoryPowerDemand;
                    remainingSupply = 0;
                    foreach (var consumer in _consumersByPriority[priority])
                    {
                        consumer.ReceivedPower = (int)(consumer.DrawRate * availiablePowerFraction); //give each consumer a fraction of what they requested (rounded down to nearest int)
                    }
                }
            }
        }
    }

    public enum Voltage
    {
        High,
        Medium,
        Low
    }

    public enum Priority //ordered from first to last so powernet foreach loops that enumerate over values of the enum go in order of highest priority down
    {
        First,
        Last
    }
}
