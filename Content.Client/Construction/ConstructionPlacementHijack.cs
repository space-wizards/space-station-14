using System.Linq;
using Content.Shared.Construction.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction
{
    public sealed class ConstructionPlacementHijack : PlacementHijack
    {
        private readonly ConstructionSystem _constructionSystem;
        private readonly ConstructionPrototype? _prototype;

        public ConstructionSystem? CurrentConstructionSystem { get { return _constructionSystem; } }
        public ConstructionPrototype? CurrentPrototype { get { return _prototype; } }

        public override bool CanRotate { get; }

        public ConstructionPlacementHijack(ConstructionSystem constructionSystem, ConstructionPrototype? prototype)
        {
            _constructionSystem = constructionSystem;
            _prototype = prototype;
            CanRotate = prototype?.CanRotate ?? true;
        }

        /// <inheritdoc />
        public override bool HijackPlacementRequest(EntityCoordinates coordinates)
        {
            if (_prototype != null)
            {
                var dir = Manager.Direction;
                _constructionSystem.SpawnGhost(_prototype, coordinates, dir);
            }
            return true;
        }

        /// <inheritdoc />
        public override bool HijackDeletion(EntityUid entity)
        {
            if (IoCManager.Resolve<IEntityManager>().HasComponent<ConstructionGhostComponent>(entity))
            {
                _constructionSystem.ClearGhost(entity.GetHashCode());
            }
            return true;
        }

        /// <inheritdoc />
        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            if (_prototype is null || !_constructionSystem.TryGetRecipePrototype(_prototype.ID, out var targetProtoId))
                return;

            if (!IoCManager.Resolve<IPrototypeManager>().TryIndex(targetProtoId, out EntityPrototype? proto))
                return;

            manager.CurrentTextures = SpriteComponent.GetPrototypeTextures(proto, IoCManager.Resolve<IResourceCache>()).ToList();
        }
    }
}
