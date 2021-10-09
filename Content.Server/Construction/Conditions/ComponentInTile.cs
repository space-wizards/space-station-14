using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Makes the condition fail if any entities on a tile have (or not) a component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class ComponentInTile : IGraphCondition, ISerializationHooks
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        /// <summary>
        ///     If true, any entity on the tile must have the component.
        ///     If false, no entity on the tile must have the component.
        /// </summary>
        [DataField("hasEntity")]
        public bool HasEntity { get; private set; }

        /// <summary>
        ///     The component name in question.
        /// </summary>
        [DataField("component")]
        public string Component { get; private set; } = string.Empty;

        public async Task<bool> Condition(IEntity entity)
        {
            if (string.IsNullOrEmpty(Component)) return false;

            var type = _componentFactory.GetRegistration(Component).Type;

            var indices = entity.Transform.Coordinates.ToVector2i(entity.EntityManager, _mapManager);
            var entities = indices.GetEntitiesInTile(entity.Transform.GridID, LookupFlags.Approximate | LookupFlags.IncludeAnchored, IoCManager.Resolve<IEntityLookup>());

            foreach (var ent in entities)
            {
                if (ent.HasComponent(type))
                    return HasEntity;
            }

            return !HasEntity;
        }

        // TODO CONSTRUCTION: Custom examine for this condition.
    }
}
