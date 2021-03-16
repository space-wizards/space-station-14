using Content.Server.GameObjects.Components.Fluids;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PuddleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            var mapManager = IoCManager.Resolve<IMapManager>();
            mapManager.TileChanged += HandleTileChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var mapManager = IoCManager.Resolve<IMapManager>();
            mapManager.TileChanged -= HandleTileChanged;
        }

        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // If this gets hammered you could probably queue up all the tile changes every tick but I doubt that would ever happen.
            foreach (var (puddle, snapGrid) in ComponentManager.EntityQuery<PuddleComponent, SnapGridComponent>(true))
            {
                // If the tile becomes space then delete it (potentially change by design)
                if (eventArgs.NewTile.GridIndex == puddle.Owner.Transform.GridID &&
                    snapGrid.Position == eventArgs.NewTile.GridIndices &&
                    eventArgs.NewTile.Tile.IsEmpty)
                {
                    puddle.Owner.Delete();
                    break; // Currently it's one puddle per tile, if that changes remove this
                }
            }
        }
    }
}
