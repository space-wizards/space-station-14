using System.Reflection;
using Content.Server.Power.Generation.Teg;
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
        INodeGroup MakeNodeGroup(NodeGroupID id);
    }

    public sealed class NodeGroupFactory : INodeGroupFactory
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

        public INodeGroup MakeNodeGroup(NodeGroupID id)
        {
            if (!_groupTypes.TryGetValue(id, out var type))
                throw new ArgumentException($"{id} did not have an associated {nameof(INodeGroup)} implementation.");

            var instance = _typeFactory.CreateInstance<INodeGroup>(type);
            instance.Create(id);
            return instance;
        }
    }

    public enum NodeGroupID : byte
    {
        Default,
        HVPower,
        MVPower,
        Apc,
        AMEngine,
        Pipe,
        WireNet,

        /// <summary>
        /// Group used by the TEG.
        /// </summary>
        /// <seealso cref="TegSystem"/>
        /// <seealso cref="TegNodeGroup"/>
        Teg,
    }
}
