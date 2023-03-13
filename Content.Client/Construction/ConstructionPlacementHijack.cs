using Content.Shared.Construction.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction
{
    public sealed class ConstructionPlacementHijack : PlacementHijack
    {
        private readonly ConstructionSystem _constructionSystem;
        private readonly IPrototypeManager _prototypeManager;
        private readonly ConstructionPrototype? _prototype;

        public override bool CanRotate { get; }

        public ConstructionPlacementHijack(ConstructionSystem constructionSystem, IPrototypeManager prototypeManager, ConstructionPrototype? prototype)
        {
            _constructionSystem = constructionSystem;
            _prototypeManager = prototypeManager;
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
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ConstructionGhostComponent? ghost))
            {
                _constructionSystem.ClearGhost(ghost.GhostId);
            }
            return true;
        }

        private bool SetManagerSprite(PlacementManager manager, ConstructionPrototype prototype)
        {
            var sprite = _constructionSystem.GetTargetNodeSprite(prototype);
            if (sprite == null)
            {
                Logger.Error("Failed to get the target node sprite of the construction prototype.");
                return false;
            }

            manager.PreparePlacementSprite(sprite);
            return true;
        }

        /// <inheritdoc />
        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            if (_prototype == null)
            {
                manager.CurrentTextures = null;
            }
            else if (!_prototype.InferSprite
                     || (_prototype.InferSprite && !SetManagerSprite(manager, _prototype)))
            {
                var frame = _prototype.Icon.DirFrame0();
                manager.CurrentTextures = new List<IDirectionalTextureProvider>{frame};
            }
        }
    }
}
