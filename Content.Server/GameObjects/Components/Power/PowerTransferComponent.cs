using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Server.Utility;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component to transfer power to nearby components, can create powernets and connect to nodes
    /// </summary>
    [RegisterComponent]
    public class PowerTransferComponent : Component, IAttackBy
    {
        public override string Name => "PowerTransfer";

        /// <summary>
        /// The powernet this component is connected to
        /// </summary>
        [ViewVariables]
        public Powernet Parent { get; set; }

        [ViewVariables]
        public bool Regenerating { get; set; } = false;

        protected override void Startup()
        {
            base.Startup();

            if (Parent == null)
            {
                SpreadPowernet();
            }
        }

        public override void OnRemove()
        {
            DisconnectFromPowernet();

            base.OnRemove();
        }

        /// <summary>
        /// Searches for local powernets to connect to, otherwise creates its own, and spreads powernet to nearby entities
        /// </summary>
        public void SpreadPowernet()
        {
            var entMgr = IoCManager.Resolve<IServerEntityManager>();
            var sgc = Owner.GetComponent<SnapGridComponent>();
            var wires = sgc.GetCardinalNeighborCells()
                .SelectMany(x => x.GetLocal()).Distinct()
                .Select(x => x.TryGetComponent<PowerTransferComponent>(out var c) ? c : null)
                .Where(x => x != null).Distinct()
                .ToArray();

            //we have no parent so lets find a partner we can join his powernet
            if (Parent == null || Regenerating)
            {
                foreach (var wire in wires)
                {
                    if (wire.CanConnectTo())
                    {
                        ConnectToPowernet(wire.Parent);
                        break;
                    }
                }

                //we couldn't find a partner so none must have spread yet, lets make our own powernet to spread
                if (Parent == null || Regenerating)
                {
                    ConnectToPowernet(new Powernet());
                }
            }

            //Find nodes intersecting us and if not already assigned to a powernet assign them to us
            var nodes = entMgr.GetEntitiesIntersecting(Owner)
                        .Select(x => x.TryGetComponent(out PowerNodeComponent pnc) ? pnc : null)
                        .Where(x => x != null);

            foreach (var node in nodes)
            {
                if (node.Parent == null)
                {
                    node.ConnectToPowernet(Parent);
                }
                else if (node.Parent.Dirty)
                {
                    node.RegeneratePowernet(Parent);
                }
            }

            //spread powernet to nearby wires which haven't got one yet, and tell them to spread as well
            foreach (var wire in wires)
            {
                if (wire.Parent == null || Regenerating)
                {
                    wire.ConnectToPowernet(Parent);
                    wire.SpreadPowernet();
                }
                else if (wire.Parent != Parent && !wire.Parent.Dirty)
                {
                    Parent.MergePowernets(wire.Parent);
                }
            }
        }

        /// <summary>
        /// Called when connecting to a new powernet, either on creation or on regeneration
        /// </summary>
        /// <param name="toconnect"></param>
        public void ConnectToPowernet(Powernet toconnect)
        {
            Parent = toconnect;
            Parent.WireList.Add(this);
            Regenerating = false;
        }

        /// <summary>
        /// Called when we are removed and telling the powernet that it is now dirty and must regenerate
        /// </summary>
        public void DisconnectFromPowernet()
        {
            Parent.WireList.Remove(this);
            Parent.Dirty = true;
            Parent = null;
        }


        public bool CanConnectTo()
        {
            return Parent != null && Parent.Dirty == false && !Regenerating;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.TryGetComponent(out ToolComponent tool)) return false;
            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Cutting)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity("CableStack", eventArgs.ClickLocation);

            if (droppedEnt.TryGetComponent<StackComponent>(out var stackComp))
                stackComp.Count = 1;

            return true;
        }
    }
}
