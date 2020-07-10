using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    public interface INodeStateFactory
    {
        /// <summary>
        ///     Performs reflection to associate <see cref="Node"/> implementations with the
        ///     string specified in their <see cref="NodeStateAttribute"/>.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Returns a new <see cref="Node"/> instance.
        /// </summary>
        BaseNodeState MakeNodeState(NodeStateID nodeStateID);
    }

    public class NodeStateFactory : INodeStateFactory
    {
        private readonly Dictionary<NodeStateID, Type> _groupTypes = new Dictionary<NodeStateID, Type>();

#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
#pragma warning restore 649

        public void Initialize()
        {
            var nodeStateTypes = _reflectionManager.GetAllChildren<BaseNodeState>();
            foreach (var nodeStateType in nodeStateTypes)
            {
                var att = nodeStateType.GetCustomAttribute<NodeStateAttribute>();
                if (att != null)
                {
                    _groupTypes.Add(att.NodeStateID, nodeStateType);
                }
            }
        }

        public BaseNodeState MakeNodeState(NodeStateID nodeStateID)
        {
            if (_groupTypes.TryGetValue(nodeStateID, out var type))
            {
                return _typeFactory.CreateInstance<BaseNodeState>(type);
            }
            throw new ArgumentException($"{nodeStateID} did not have an associated {nameof(BaseNodeState)}.");
        }
    }
}
