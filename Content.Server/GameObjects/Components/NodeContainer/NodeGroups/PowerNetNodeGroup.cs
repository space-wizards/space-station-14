using Content.Server.GameObjects.Components.NewPower;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Interface for null object of <see cref="PowerNetNodeGroup"/>
    /// </summary>
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

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower, NodeGroupID.LVPower)]
    public class PowerNetNodeGroup : NodeGroup, IPowerNet
    {
        private readonly Dictionary<INode, List<BasePowerComponent>> _powerComponents = new Dictionary<INode, List<BasePowerComponent>>();

        [ViewVariables]
        private readonly List<PowerSupplierComponent> _suppliers = new List<PowerSupplierComponent>();

        [ViewVariables]
        private int _totalSupply = 0;

        [ViewVariables]
        private readonly Dictionary<Priority, List<PowerConsumerComponent>> _consumersByPriority = new Dictionary<Priority, List<PowerConsumerComponent>>();

        [ViewVariables]
        private readonly Dictionary<Priority, int> _drawByPriority = new Dictionary<Priority, int>();

        private bool _supressPowerRecalculation = false;

        public PowerNetNodeGroup()
        {
            foreach(Priority priority in Enum.GetValues(typeof(Priority)))
            {
                _consumersByPriority.Add(priority, new List<PowerConsumerComponent>());
                _drawByPriority.Add(priority, 0);
            }
        }

        protected override void OnAddNode(INode node)
        {
            var newPowerComponents = node.Owner
                .GetAllComponents<BasePowerComponent>()
                .Where(powerComp => (NodeGroupID) powerComp.Voltage == node.NodeGroupID)
                .ToList();
            _powerComponents.Add(node, newPowerComponents);
            foreach (var powerComponent in newPowerComponents)
            {
                powerComponent.PowerNet = this;
            }
        }

        protected override void OnRemoveNode(INode node)
        {
            foreach (var powerComponent in _powerComponents[node])
            {
                powerComponent.ClearPowerNet();
            }
            _powerComponents.Remove(node);
        }

        protected override void BeforeRemake()
        {
            _supressPowerRecalculation = true;
        }

        protected override void AfterRemake()
        {
            _supressPowerRecalculation = false;
            UpdateConsumerReceivedPower();
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

        #region IPowerNet

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

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            _consumersByPriority[consumer.Priority].Add(consumer);
            _drawByPriority[consumer.Priority] += consumer.DrawRate;
            UpdateConsumerReceivedPower();
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            _consumersByPriority[consumer.Priority].Add(consumer);
            _drawByPriority[consumer.Priority] -= consumer.DrawRate;
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

        public void UpdateSupplierSupply(PowerSupplierComponent supplier, int oldSupplyRate, int newSupplyRate)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _totalSupply -= oldSupplyRate;
            _totalSupply += newSupplyRate;
        }

        public void UpdateConsumerDraw(PowerConsumerComponent consumer, int oldDrawRate, int newDrawRate)
        {
            Debug.Assert(_consumersByPriority[consumer.Priority].Contains(consumer));
            _drawByPriority[consumer.Priority] -= oldDrawRate;
            _drawByPriority[consumer.Priority] += newDrawRate;
        }

        public void UpdateConsumerPriority(PowerConsumerComponent consumer, Priority oldPriority, Priority newPriority)
        {
            Debug.Assert(_consumersByPriority[oldPriority].Contains(consumer));
            _consumersByPriority[oldPriority].Remove(consumer);
            _drawByPriority[oldPriority] -= consumer.DrawRate;
            _consumersByPriority[newPriority].Add(consumer);
            _drawByPriority[newPriority] += consumer.DrawRate;
        }

        #endregion
    }
}
