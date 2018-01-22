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
    //Component to transfer power to nearby components, can create powernets and connect to nodes
    public class PowerTransferComponent : Component
    {
        public override string Name => "PowerTransfer";

        public Powernet Parent;

        public override void Initialize()
        {
            if(Parent == null)
            {
                SpreadPowernet();
            }
        }

        public void SpreadPowernet()
        {
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var wires = _emanager.GetEntitiesInRange(Owner, 1.1f) //arbitrarily low, just scrape things //wip
                        .Where(x => x.HasComponent<PowerTransferComponent>());

            //we have no parent so lets find a partner we can join his powernet
            if(Parent == null)
            {
                foreach (var wire in wires)
                {
                    var ptc = wire.GetComponent<PowerTransferComponent>();
                    if (ptc.Parent != null)
                    {
                        ConnectToPowernet(Parent);
                        break;
                    }
                }

                //we couldn't find a partner so none must have spread yet, lets make our own powernet to spread
                if (Parent == null)
                {
                    var powernew = new Powernet();
                    ConnectToPowernet(powernew);
                }
            }

            //TODO: code to find nodes that intersect our bounding box here and add them

            //spread powernet to nearby wires which haven't got one yet, and tell them to spread as well
            foreach (var wire in wires)
            {
                var ptc = wire.GetComponent<PowerTransferComponent>();
                if (ptc.Parent == null)
                {
                    ptc.ConnectToPowernet(Parent);
                    SpreadPowernet();
                }
                else if(ptc.Parent != Parent)
                {
                    Parent.MergePowernets(ptc.Parent);
                }
            }
        }

        public void ConnectToPowernet(Powernet toconnect)
        {
            Parent = toconnect;
            Parent.Wirelist.Add(this);
        }

        public void DisconnectFromPowernet()
        {
            Parent.Wirelist.Remove(this);
            Parent = null;
        }
    }
}
