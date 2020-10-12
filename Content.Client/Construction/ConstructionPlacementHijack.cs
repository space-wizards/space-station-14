using Content.Client.GameObjects.Components.Construction;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Construction
{
    public sealed class ConstructionPlacementHijack : PlacementHijack
    {
        private readonly ConstructionSystem _constructionSystem;
        private readonly ConstructionPrototype _prototype;

        public override bool CanRotate { get; }

        public ConstructionPlacementHijack(ConstructionSystem constructionSystem, ConstructionPrototype prototype)
        {
            _constructionSystem = constructionSystem;
            _prototype = prototype;
            CanRotate = prototype.CanRotate;
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
        public override bool HijackDeletion(IEntity entity)
        {
            if (entity.TryGetComponent(out ConstructionGhostComponent ghost))
            {
                _constructionSystem.ClearGhost(ghost.GhostID);
            }
            return true;
        }

        /// <inheritdoc />
        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            manager.CurrentBaseSprite = _prototype.Icon.DirFrame0();
        }
    }
}
