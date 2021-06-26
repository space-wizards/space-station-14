#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    public abstract class BaseNetConnectorComponent<TNetType> : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public Voltage Voltage { get => _voltage; set => SetVoltage(value); }
        [DataField("voltage")]
        private Voltage _voltage = Voltage.High;

        [ViewVariables]
        public TNetType Net { get => _net; set => SetNet(value); }
        private TNetType _net = default!; //set in OnAdd()

        protected abstract TNetType NullNet { get; }

        [ViewVariables]
        private bool _needsNet = true;

        protected override void OnAdd()
        {
            base.OnAdd();
            _net = NullNet;
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_needsNet)
            {
                TryFindAndSetNet();
            }
        }

        protected override void OnRemove()
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
            _net = NullNet;
            _needsNet = true;
        }

        protected abstract void AddSelfToNet(TNetType net);

        protected abstract void RemoveSelfFromNet(TNetType net);

        private bool TryFindNet([NotNullWhen(true)] out TNetType? foundNet)
        {
            if (Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                var compatibleNet = container.Nodes.Values
                    .Where(node => node.NodeGroupID == (NodeGroupID) Voltage)
                    .Select(node => node.NodeGroup)
                    .OfType<TNetType>()
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

        private void SetNet(TNetType newNet)
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
        Apc = NodeGroupID.Apc,
    }
}
