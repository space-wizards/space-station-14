using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.NewPower
{
    public abstract class BaseNetConnectorComponent<T> : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public Voltage Voltage { get => _voltage; set => SetVoltage(value); }
        private Voltage _voltage;

        [ViewVariables]
        public T Net { get => _net; set => SetNet(value); }
        private T _net;

        [ViewVariables]
        private bool _needsNet = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _voltage, "voltage", Voltage.High);
        }

        public override void Initialize()
        {
            base.Initialize();
            _net = GetNullNet();
            if (_needsNet)
            {
                TryFindAndSetNet();
            }
        }

        public override void OnRemove()
        {
            ClearNet();
            base.OnRemove();
        }

        public void TryFindAndSetNet()
        {
            if (TryFindNet(out var net))
            {
                Net = net;
            }
        }

        public void ClearNet()
        {
            RemoveSelfFromNet(_net);
            _net = GetNullNet();
            _needsNet = true;
        }

        protected abstract T GetNullNet();

        protected abstract void AddSelfToNet(T net);

        protected abstract void RemoveSelfFromNet(T net);

        private bool TryFindNet(out T foundNet)
        {
            if (Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                var compatibleNet = container.Nodes
                    .Where(node => node.NodeGroupID == (NodeGroupID) Voltage)
                    .Select(node => node.NodeGroup)
                    .OfType<T>()
                    .FirstOrDefault();

                if (compatibleNet != null)
                {
                    foundNet = compatibleNet;
                    return true;
                }
            }
            foundNet = default;
            return false;
        }

        private void SetNet(T newNet)
        {
            RemoveSelfFromNet(_net);
            AddSelfToNet(newNet);
            _net = newNet;
            _needsNet = false;
        }

        private void SetVoltage(Voltage newVoltage)
        {
            ClearNet();
            _voltage = newVoltage;
            TryFindAndSetNet();
        }
    }

    public enum Voltage
    {
        High = NodeGroupID.HVPower,
        Medium = NodeGroupID.MVPower,
        Low = NodeGroupID.LVPower,
    }
}
