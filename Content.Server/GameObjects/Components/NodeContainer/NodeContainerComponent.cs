#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager.Attributes;
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
        public IReadOnlyDictionary<string, Node> Nodes => _nodes;

        [DataField("nodes")]
        private readonly Dictionary<string, Node> _nodes = new();

        [DataField("examinable")]
        private bool _examinable = false;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var node in _nodes.Values)
            {
                node.Initialize(Owner);
            }
        }

        protected override void Startup()
        {
            base.Startup();
            foreach (var node in _nodes.Values)
            {
                node.OnContainerStartup();
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var node in _nodes.Values)
            {
                node.OnContainerShutdown();
            }
        }

        public void AnchorUpdate()
        {
            foreach (var node in Nodes.Values)
            {
                node.AnchorUpdate();
            }
        }

        public T GetNode<T>(string identifier) where T : Node
        {
            return (T)_nodes[identifier];
        }

        public bool TryGetNode<T>(string identifier, [NotNullWhen(true)] out T? node) where T : Node
        {
            if (_nodes.TryGetValue(identifier, out var n) && n is T t)
            {
                node = t;
                return true;
            }

            node = null;
            return false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_examinable || !inDetailsRange) return;

            foreach (var node in Nodes.Values)
            {
                if (node == null) continue;
                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=orange]HV cables[/color].\n"));
                        break;
                    case NodeGroupID.MVPower:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=yellow]MV cables[/color].\n"));
                        break;
                    case NodeGroupID.Apc:
                        message.AddMarkup(
                            Loc.GetString("It has a connector for [color=green]APC cables[/color].\n"));
                        break;
                }
            }
        }
    }
}
