using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.Power.Components
{
    // TODO find a way to just remove this or turn it into one component.
    // Component interface queries require enumerating over ALL of an entities components.
    // So BaseNetConnectorNodeGroup<TNetType> is slow as shit.
    public interface IBaseNetConnectorComponent<in TNetType>
    {
        public TNetType? Net { set; }
        public Voltage Voltage { get; }
        public string? NodeId { get; }
    }

    public abstract partial class BaseNetConnectorComponent<TNetType> : Component, IBaseNetConnectorComponent<TNetType>
        where TNetType : class
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public Voltage Voltage { get => _voltage; set => SetVoltage(value); }
        [DataField("voltage")]
        private Voltage _voltage = Voltage.High;

        [ViewVariables]
        public TNetType? Net { get => _net; set => SetNet(value); }
        private TNetType? _net;

        [ViewVariables] public bool NeedsNet => _net != null;

        [DataField("node")] public string? NodeId { get; set; }

        public void TryFindAndSetNet()
        {
            if (TryFindNet(out var net))
            {
                Net = net;
            }
        }

        public void ClearNet()
        {
            if (_net != null)
            {
                RemoveSelfFromNet(_net);
                _net = null;
            }
        }

        protected abstract void AddSelfToNet(TNetType net);

        protected abstract void RemoveSelfFromNet(TNetType net);

        private bool TryFindNet([NotNullWhen(true)] out TNetType? foundNet)
        {
            if (_entMan.TryGetComponent(Owner, out NodeContainerComponent? container))
            {
                var compatibleNet = container.Nodes.Values
                    .Where(node => (NodeId == null || NodeId == node.Name) && node.NodeGroupID == (NodeGroupID) Voltage)
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

        private void SetNet(TNetType? newNet)
        {
            if (_net != null)
                RemoveSelfFromNet(_net);

            if (newNet != null)
                AddSelfToNet(newNet);

            _net = newNet;
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
