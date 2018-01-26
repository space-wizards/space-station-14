using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that connects to the powernet
    /// </summary>
    public class PowerNodeComponent : Component
    {
        public override string Name => "PowerNode";
        
        /// <summary>
        /// The powernet this node is connected to
        /// </summary>
        public Powernet Parent;

        /// <summary>
        /// An event handling when this node connects to a powernet
        /// </summary>
        public event EventHandler<PowernetEventArgs> OnPowernetConnect;

        /// <summary>
        /// An event handling when this node disconnects from a powernet
        /// </summary>
        public event EventHandler<PowernetEventArgs> OnPowernetDisconnect;

        public override void Initialize()
        {
            TryCreatePowernetConnection();
        }

        public void TryCreatePowernetConnection()
        {
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var wires = _emanager.GetEntitiesIntersecting(Owner)
                        .Where(x => x.HasComponent<PowerTransferComponent>())
                        .OrderByDescending(x => (x.GetComponent<TransformComponent>().WorldPosition - position).Length);
            ConnectToPowernet(wires.First().GetComponent<PowerTransferComponent>().Parent);
        }

        public void ConnectToPowernet(Powernet toconnect)
        {
            Parent = toconnect;
            Parent.Nodelist.Add(this);
            OnPowernetConnect?.Invoke(this, new PowernetEventArgs(Parent));
        }

        public void DisconnectFromPowernet()
        {
            Parent.Nodelist.Remove(this);
            Parent = null;
            OnPowernetDisconnect?.Invoke(this, new PowernetEventArgs(Parent));
        }
    }

    public class PowernetEventArgs : EventArgs
    {
        public PowernetEventArgs(Powernet powernet)
        {
            Powernet = powernet;
        }

        public Powernet Powernet { get; }
    }
}
