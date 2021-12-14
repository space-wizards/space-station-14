using System.Collections.Generic;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Construction
{
    public sealed class ConstructionPlacementHijack : PlacementHijack
    {
        private readonly ConstructionSystem _constructionSystem;
        private readonly ConstructionPrototype? _prototype;

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
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ConstructionGhostComponent? ghost))
            {
                _constructionSystem.ClearGhost(ghost.GhostId);
            }
            return true;
        }

        /// <inheritdoc />
        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            var frame = _prototype?.Icon.DirFrame0();
            if (frame == null)
            {
                manager.CurrentTextures = null;
            }
            else
            {
                manager.CurrentTextures = new List<IDirectionalTextureProvider> {frame};
            }
        }
    }
}
