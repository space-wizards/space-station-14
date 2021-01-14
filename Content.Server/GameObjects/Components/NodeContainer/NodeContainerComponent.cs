using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer
{
    /// <summary>
    ///     Creates and maintains a set of <see cref="Node"/>s.
    /// </summary>
    [RegisterComponent]
    public class NodeContainerComponent : Component, IExamine
    {
        public override string Name => "NodeContainer";

        [ViewVariables]
        public IReadOnlyList<Node> Nodes => _nodes;
        private List<Node> _nodes = new();
        private bool _examinable;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _nodes, "nodes", new List<Node>());
            serializer.DataField(ref _examinable, "examinable", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var node in _nodes)
            {
                node.Initialize(Owner);
            }
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var node in _nodes)
            {
                node.OnContainerStartup();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    AnchorUpdate();
                    break;
            }
        }

        public override void OnRemove()
        {
            foreach (var node in _nodes)
            {
                node.OnContainerRemove();
            }
            base.OnRemove();
        }

        private void AnchorUpdate()
        {
            foreach (var node in Nodes)
            {
                node.AnchorUpdate();
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_examinable || !inDetailsRange) return;

            for (var i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node == null) continue;
                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=orange]HV cables[/color]."));
                        break;
                    case NodeGroupID.MVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=yellow]MV cables[/color]."));
                        break;
                    case NodeGroupID.Apc:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=green]APC cables[/color]."));
                        break;
                }

                if(i != Nodes.Count - 1)
                    message.AddMarkup("\n");
            }
        }
    }
}
