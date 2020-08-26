using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
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
        INodeGroup MakeNodeGroup(NodeGroupID nodeGroupType);
    }

    public class NodeGroupFactory : INodeGroupFactory
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

        private readonly Dictionary<NodeGroupID, Type> _groupTypes = new Dictionary<NodeGroupID, Type>();

        public void Initialize()
        {
            var nodeGroupTypes = _reflectionManager.GetAllChildren<BaseNodeGroup>();
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

        public INodeGroup MakeNodeGroup(NodeGroupID nodeGroupType)
        {
            if (_groupTypes.TryGetValue(nodeGroupType, out var type))
            {
                return _typeFactory.CreateInstance<INodeGroup>(type);
            }
            throw new ArgumentException($"{nodeGroupType} did not have an associated {nameof(INodeGroup)}.");
        }
    }

    public enum NodeGroupID
    {
        Default,
        HVPower,
        MVPower,
        Apc,
    }
}
