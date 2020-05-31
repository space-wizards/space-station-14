using Content.Server.GameObjects.Components.NewPower;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.ViewVariables;
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

        void AddConsumer(PowerConsumerComponent consumer);

        void RemoveConsumer(PowerConsumerComponent consumer);
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
        private readonly List<PowerConsumerComponent> _consumers = new List<PowerConsumerComponent>();

        [ViewVariables]
        private int _totalDraw = 0;

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

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            _suppliers.Add(supplier);
            _totalSupply += supplier.SupplyRate;
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            Debug.Assert(_suppliers.Contains(supplier));
            _suppliers.Remove(supplier);
            _totalSupply -= supplier.SupplyRate;
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            _consumers.Add(consumer);
            _totalDraw += consumer.DrawRate;
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            Debug.Assert(_consumers.Contains(consumer));
            _consumers.Remove(consumer);
            _totalDraw -= consumer.DrawRate;
        }
    }
}
