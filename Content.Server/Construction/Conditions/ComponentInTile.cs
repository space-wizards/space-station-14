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
    public class ComponentInTile : IGraphCondition
    {
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

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Component)) return false;

            var type = IoCManager.Resolve<IComponentFactory>().GetRegistration(Component).Type;

            var transform = entityManager.GetComponent<ITransformComponent>(uid);
            var indices = transform.Coordinates.ToVector2i(entityManager, IoCManager.Resolve<IMapManager>());
            var entities = indices.GetEntitiesInTile(transform.GridID, LookupFlags.Approximate | LookupFlags.IncludeAnchored, IoCManager.Resolve<IEntityLookup>());

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
