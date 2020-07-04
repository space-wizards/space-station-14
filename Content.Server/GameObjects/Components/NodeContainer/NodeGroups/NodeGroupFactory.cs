using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

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
        private readonly Dictionary<NodeGroupID, Type> _groupTypes = new Dictionary<NodeGroupID, Type>();

#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
#pragma warning restore 649

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
