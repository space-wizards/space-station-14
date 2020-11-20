using System;
using System.Collections.Generic;
using System.Diagnostics;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    public interface IPowerNet
    {
        void AddSupplier(PowerSupplierComponent supplier);

        void RemoveSupplier(PowerSupplierComponent supplier);

        void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupplyRate, int newSupplyRate);

        void AddConsumer(PowerConsumerComponent consumer);

        void RemoveConsumer(PowerConsumerComponent consumer);

        void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDrawRate, int newDrawRate);

        void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority);

        void UpdateConsumerReceivedPower();
    }

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower)]
    public class PowerNetNodeGroup : BaseNetConnectorNodeGroup<BasePowerNetComponent, IPowerNet>, IPowerNet
    {
        [Dependency] private readonly IPowerNetManager _powerNetManager = default!;

        [ViewVariables]
        private readonly List<PowerSupplierComponent> _suppliers = new();

        [ViewVariables]
        private int _totalSupply = 0;

        [ViewVariables]
        private readonly Dictionary<Priority, List<PowerConsumerComponent>> _consumersByPriority = new();

        [ViewVariables]
        private readonly Dictionary<Priority, int> _drawByPriority = new();

        public static readonly IPowerNet NullNet = new NullPowerNet();

        public PowerNetNodeGroup()
        {
            foreach (Priority priority in Enum.GetValues(typeof(Priority)))
            {
                _consumersByPriority.Add(priority, new List<PowerConsumerComponent>());
                _drawByPriority.Add(priority, 0);
            }
        }

        protected override void SetNetConnectorNet(BasePowerNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        #region IPowerNet Methods

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            _suppliers.Add(supplier);
            _totalSupply += supplier.SupplyRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _suppliers.Remove(supplier);
            _totalSupply -= supplier.SupplyRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupplyRate, int newSupplyRate)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _totalSupply -= oldSupplyRate;
            _totalSupply += newSupplyRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            _consumersByPriority[consumer.Priority].Add(consumer);
            _drawByPriority[consumer.Priority] += consumer.DrawRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            consumer.ReceivedPower = 0;
            _consumersByPriority[consumer.Priority].Remove(consumer);
            _drawByPriority[consumer.Priority] -= consumer.DrawRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDrawRate, int newDrawRate)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            _drawByPriority[consumer.Priority] -= oldDrawRate;
            _drawByPriority[consumer.Priority] += newDrawRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority)
        {
            Debug.Assert(_consumersByPriority[oldPriority].Contains(consumer));
            _consumersByPriority[oldPriority].Remove(consumer);
            _drawByPriority[oldPriority] -= consumer.DrawRate;
            _consumersByPriority[newPriority].Add(consumer);
            _drawByPriority[newPriority] += consumer.DrawRate;
            _powerNetManager.AddDirtyPowerNet(this);
        }

        public void UpdateConsumerReceivedPower()
        {
            var remainingSupply = _totalSupply;
            foreach (Priority priority in Enum.GetValues(typeof(Priority)))
            {
                var categoryPowerDemand = _drawByPriority[priority];
                if (remainingSupply >= categoryPowerDemand) //can fully power all in category
                {
                    remainingSupply -= categoryPowerDemand;
                    foreach (var consumer in _consumersByPriority[priority])
                    {
                        consumer.ReceivedPower = consumer.DrawRate;
                    }
                }
                else //cannot fully power all, split power
                {
                    var availiablePowerFraction = (float) remainingSupply / categoryPowerDemand;
                    remainingSupply = 0;
                    foreach (var consumer in _consumersByPriority[priority])
                    {
                        consumer.ReceivedPower = (int) (consumer.DrawRate * availiablePowerFraction); //give each consumer a fraction of what they requested (rounded down to nearest int)
                    }
                }
            }
        }

        #endregion

        private class NullPowerNet : IPowerNet
        {
            public void AddConsumer(PowerConsumerComponent consumer) { }
            public void AddSupplier(PowerSupplierComponent supplier) { }
            public void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupplyRate, int newSupplyRate) { }
            public void RemoveConsumer(PowerConsumerComponent consumer) { }
            public void RemoveSupplier(PowerSupplierComponent supplier) { }
            public void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDrawRate, int newDrawRate) { }
            public void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority) { }
            public void UpdateConsumerReceivedPower() { }
        }
    }
}
