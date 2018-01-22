using OpenTK;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Power
{
    public class PowerNodeComponent : Component
    {
        public override string Name => "PowerNode";

        public Powernet Parent;

        public event EventHandler<PowernetEventArgs> OnPowernetConnect;
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
