using Content.Server.GameObjects.Components.Fluids;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.TileChanged += HandleTileChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.TileChanged -= HandleTileChanged;
        }

        //TODO: Replace all this with an Unanchored event that deletes the puddle
        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // If this gets hammered you could probably queue up all the tile changes every tick but I doubt that would ever happen.
            foreach (var puddle in ComponentManager.EntityQuery<PuddleComponent>(true))
            {
                // If the tile becomes space then delete it (potentially change by design)
                var puddleTransform = puddle.Owner.Transform;
                if(!puddleTransform.Anchored)
                    continue;

                var grid = _mapManager.GetGrid(puddleTransform.GridID);
                if (eventArgs.NewTile.GridIndex == puddle.Owner.Transform.GridID &&
                    grid.TileIndicesFor(puddleTransform.Coordinates) == eventArgs.NewTile.GridIndices &&
                    eventArgs.NewTile.Tile.IsEmpty)
                {
                    puddle.Owner.Delete();
                    break; // Currently it's one puddle per tile, if that changes remove this
                }
            }
        }
    }
}
