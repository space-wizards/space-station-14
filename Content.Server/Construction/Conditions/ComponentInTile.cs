using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Makes the condition fail if any entities on a tile have (or not) a component.
    /// </summary>
    [UsedImplicitly]
    public class ComponentInTile : IEdgeCondition
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public ComponentInTile()
        {
            IoCManager.InjectDependencies(this);
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Component, "component", string.Empty);
            serializer.DataField(this, x => x.HasEntity, "hasEntity", true);
        }

        /// <summary>
        ///     If true, any entity on the tile must have the component.
        ///     If false, no entity on the tile must have the component.
        /// </summary>
        public bool HasEntity { get; private set; }

        /// <summary>
        ///     The component name in question.
        /// </summary>
        public string Component { get; private set; }

        public async Task<bool> Condition(IEntity entity)
        {
            if (string.IsNullOrEmpty(Component)) return false;

            var type = _componentFactory.GetRegistration(Component).Type;

            var indices = entity.Transform.Coordinates.ToVector2i(entity.EntityManager, _mapManager);
            var entities = indices.GetEntitiesInTile(entity.Transform.GridID, true, entity.EntityManager);

            foreach (var ent in entities)
            {
                if (ent.HasComponent(type))
                    return HasEntity;
            }

            return !HasEntity;
        }
    }
}
