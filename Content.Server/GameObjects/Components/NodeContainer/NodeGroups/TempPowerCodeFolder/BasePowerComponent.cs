using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    public abstract class BasePowerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public Voltage Voltage { get => _voltage; set => SetVoltage(value); }
        private Voltage _voltage;

        [ViewVariables]
        public IPowerNet PowerNet { get => _powerNet; set => SetPowerNet(value); }
        private IPowerNet _powerNet = NullNet;

        [ViewVariables]
        private bool _needsPowerNet = true;

        private static readonly IPowerNet NullNet = new NullPowerNet();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _voltage, "voltage", Voltage.High);
        }

        public override void OnAdd()
        {
            base.OnAdd();
            if (_needsPowerNet)
            {
                TryFindAndSetPowerNet();
            }
        }

        public override void OnRemove()
        {
            ClearPowerNet();
            base.OnRemove();
        }

        public void TryFindAndSetPowerNet()
        {
            if (TryFindPowerNet(out var powerNet))
            {
                PowerNet = powerNet;
            }
        }

        public void ClearPowerNet()
        {
            RemoveSelfFromNet(_powerNet);
            _powerNet = NullNet;
            _needsPowerNet = true;
        }

        protected abstract void AddSelfToNet(IPowerNet powerNet);

        protected abstract void RemoveSelfFromNet(IPowerNet powerNet);

        private bool TryFindPowerNet(out IPowerNet foundNet)
        {
            if (Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                var compatibleNet = container.Nodes
                    .Where(node => node.NodeGroupID == (NodeGroupID) Voltage)
                    .Select(node => node.NodeGroup)
                    .OfType<IPowerNet>()
                    .FirstOrDefault();

                if (compatibleNet != null)
                {
                    foundNet = compatibleNet;
                    return true;
                }
            }
            foundNet = null;
            return false;
        }

        private void SetPowerNet(IPowerNet newPowerNet)
        {
            RemoveSelfFromNet(_powerNet);
            AddSelfToNet(newPowerNet);
            _powerNet = newPowerNet;
            _needsPowerNet = false;
        }

        private void SetVoltage(Voltage newVoltage)
        {
            ClearPowerNet();
            _voltage = newVoltage;
            TryFindAndSetPowerNet();
        }

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

    public enum Voltage
    {
        High = NodeGroupID.HVPower,
        Medium = NodeGroupID.MVPower,
        Low = NodeGroupID.LVPower,
    }
}
