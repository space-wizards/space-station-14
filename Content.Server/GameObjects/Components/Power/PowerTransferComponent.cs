using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System.Linq;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component to transfer power to nearby components, can create powernets and connect to nodes
    /// </summary>
    public class PowerTransferComponent : Component
    {
        public override string Name => "PowerTransfer";

        /// <summary>
        /// The powernet this component is connected to
        /// </summary>
        public Powernet Parent;

        public bool Regenerating { get; set; } = false;

        public override void Initialize()
        {
            if(Parent == null)
            {
                SpreadPowernet();
            }
        }

        /// <summary>
        /// Searches for local powernets to connect to, otherwise creates its own, and spreads powernet to nearby entities
        /// </summary>
        public void SpreadPowernet()
        {
            var _emanager = IoCManager.Resolve<IServerEntityManager>();
            var position = Owner.GetComponent<TransformComponent>().WorldPosition;
            var wires = _emanager.GetEntitiesInRange(Owner, 1.1f) //arbitrarily low, just scrape things //wip
                        .Where(x => x.HasComponent<PowerTransferComponent>());

            //we have no parent so lets find a partner we can join his powernet
            if(Parent == null || Regenerating)
            {
                foreach (var wire in wires)
                {
                    var ptc = wire.GetComponent<PowerTransferComponent>();
                    if (ptc.CanConnectTo())
                    {
                        ConnectToPowernet(Parent);
                        break;
                    }
                }

                //we couldn't find a partner so none must have spread yet, lets make our own powernet to spread
                if (Parent == null || Regenerating)
                {
                    var powernew = new Powernet();
                    ConnectToPowernet(powernew);
                }
            }

            //Find nodes intersecting us and if not already assigned to a powernet assign them to us
            var nodes = _emanager.GetEntitiesIntersecting(Owner)
                        .Where(x => x.HasComponent<PowerNodeComponent>())
                        .Select(x => x.GetComponent<PowerNodeComponent>());

            foreach(var node in nodes)
            {
                if(node.Parent == null)
                {
                    node.ConnectToPowernet(Parent);
                }
                else if(node.Parent.Dirty)
                {
                    node.RegeneratePowernet(Parent);
                }
            }

            //spread powernet to nearby wires which haven't got one yet, and tell them to spread as well
            foreach (var wire in wires)
            {
                var ptc = wire.GetComponent<PowerTransferComponent>();
                if (ptc.Parent == null || Regenerating)
                {
                    ptc.ConnectToPowernet(Parent);
                    SpreadPowernet();
                }
                else if(ptc.Parent != Parent && !ptc.Parent.Dirty)
                {
                    Parent.MergePowernets(ptc.Parent);
                }
            }
        }

        public void ConnectToPowernet(Powernet toconnect)
        {
            Parent = toconnect;
            Parent.Wirelist.Add(this);
            Regenerating = false;
        }

        public void DisconnectFromPowernet()
        {
            Parent.Wirelist.Remove(this);
            Parent = null;
        }

        public bool CanConnectTo()
        {
            return Parent != null && Parent.Dirty == false && !Regenerating;
        }
    }
}
