#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.Server.NodeContainer.NodeGroups
{
    public interface INodeGroupFactory
    {
        /// <summary>
        ///     Performs reflection to associate <see cref="INodeGroup"/> implementations with the
        ///     string specified in their <see cref="NodeGroupAttribute"/>.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Returns a new <see cref="INodeGroup"/> instance.
        /// </summary>
        INodeGroup MakeNodeGroup(Node sourceNode);
    }

    public class NodeGroupFactory : INodeGroupFactory
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

        private readonly Dictionary<NodeGroupID, Type> _groupTypes = new();

        public void Initialize()
        {
            var nodeGroupTypes = _reflectionManager.GetAllChildren<INodeGroup>();
            foreach (var nodeGroupType in nodeGroupTypes)
            {
                var att = nodeGroupType.GetCustomAttribute<NodeGroupAttribute>();
                if (att != null)
                {
                    foreach (var groupID in att.NodeGroupIDs)
                    {
                        _groupTypes.Add(groupID, nodeGroupType);
                    }
                }
            }
        }

        public INodeGroup MakeNodeGroup(Node sourceNode)
        {
            if (_groupTypes.TryGetValue(sourceNode.NodeGroupID, out var type))
            {
                var nodeGroup = _typeFactory.CreateInstance<INodeGroup>(type);
                nodeGroup.Initialize(sourceNode);
                return nodeGroup;
            }
            throw new ArgumentException($"{sourceNode.NodeGroupID} did not have an associated {nameof(INodeGroup)}.");
        }
    }

    public enum NodeGroupID
    {
        Default,
        HVPower,
        MVPower,
        Apc,
        AMEngine,
        Pipe,
        WireNet
    }
}
