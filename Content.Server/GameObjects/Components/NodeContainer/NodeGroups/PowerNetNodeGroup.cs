using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    }

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower)]
    public class PowerNetNodeGroup : BaseNetConnectorNodeGroup<BasePowerNetComponent, IPowerNet>, IPowerNet
    {
        [ViewVariables]
        private readonly List<PowerSupplierComponent> _suppliers = new List<PowerSupplierComponent>();

        [ViewVariables]
        private int _totalSupply = 0;

        [ViewVariables]
        private readonly Dictionary<Priority, List<PowerConsumerComponent>> _consumersByPriority = new Dictionary<Priority, List<PowerConsumerComponent>>();

        [ViewVariables]
        private readonly Dictionary<Priority, int> _drawByPriority = new Dictionary<Priority, int>();

        [ViewVariables]
        private bool _supressPowerRecalculation = false;

        public static readonly IPowerNet NullNet = new NullPowerNet();

        public PowerNetNodeGroup()
        {
            foreach(Priority priority in Enum.GetValues(typeof(Priority)))
            {
                _consumersByPriority.Add(priority, new List<PowerConsumerComponent>());
                _drawByPriority.Add(priority, 0);
            }
        }

        #region BaseNodeGroup Overrides

        protected override void SetNetConnectorNet(BasePowerNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public override void BeforeCombine()
        {
            _supressPowerRecalculation = true;
        }

        public override void AfterCombine()
        {
            _supressPowerRecalculation = false;
            UpdateConsumerReceivedPower();
        }

        protected override void BeforeRemake()
        {
            _supressPowerRecalculation = true;
        }

        public override void BeforeRemakeSpread()
        {
            _supressPowerRecalculation = true;
        }

        public override void AfterRemakeSpread()
        {
            _supressPowerRecalculation = false;
            UpdateConsumerReceivedPower();
        }

        #endregion

        #region IPowerNet Methods

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            _suppliers.Add(supplier);
            _totalSupply += supplier.SupplyRate;
            UpdateConsumerReceivedPower();
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _suppliers.Remove(supplier);
            _totalSupply -= supplier.SupplyRate;
            UpdateConsumerReceivedPower();
        }

        public void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupplyRate, int newSupplyRate)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _totalSupply -= oldSupplyRate;
            _totalSupply += newSupplyRate;
            UpdateConsumerReceivedPower();
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            _consumersByPriority[consumer.Priority].Add(consumer);
            _drawByPriority[consumer.Priority] += consumer.DrawRate;
            UpdateConsumerReceivedPower();
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            consumer.ReceivedPower = 0;
            _consumersByPriority[consumer.Priority].Remove(consumer);
            _drawByPriority[consumer.Priority] -= consumer.DrawRate;
            UpdateConsumerReceivedPower();
        }

        public void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDrawRate, int newDrawRate)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            _drawByPriority[consumer.Priority] -= oldDrawRate;
            _drawByPriority[consumer.Priority] += newDrawRate;
            UpdateConsumerReceivedPower();
        }

        public void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority)
        {
            Debug.Assert(_consumersByPriority[oldPriority].Contains(consumer));
            _consumersByPriority[oldPriority].Remove(consumer);
            _drawByPriority[oldPriority] -= consumer.DrawRate;
            _consumersByPriority[newPriority].Add(consumer);
            _drawByPriority[newPriority] += consumer.DrawRate;
            UpdateConsumerReceivedPower();
        }

        private void UpdateConsumerReceivedPower()
        {
            if (_supressPowerRecalculation)
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
        }
    }
}
