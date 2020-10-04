using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.Construction;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
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

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Component, "component", string.Empty);
            serializer.DataField(this, x => x.Value, "hasEntity", true);

            IoCManager.InjectDependencies(this);
        }

        public bool Value { get; private set; }

        public string Component { get; private set; }

        public async Task<bool> Condition(IEntity entity)
        {
            if (string.IsNullOrEmpty(Component)) return false;

            var type = _componentFactory.GetRegistration(Component).Type;

            foreach (var ent in entity.Transform.Coordinates.ToMapIndices(entity.EntityManager, _mapManager).GetEntitiesInTile(entity.Transform.GridID, true, entity.EntityManager))
            {
                if (ent.HasComponent(type) && Value)
                    return true;

                if (ent.HasComponent(type) && !Value)
                    return false;
            }

            return true;
        }
    }
}
