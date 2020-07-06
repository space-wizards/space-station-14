using Content.Server.GameObjects.Components.Fluids;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public class PuddleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(PuddleComponent));
            var mapManager = IoCManager.Resolve<IMapManager>();
            mapManager.TileChanged += HandleTileChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var mapManager = IoCManager.Resolve<IMapManager>();
            mapManager.TileChanged -= HandleTileChanged;
        }

        private void HandleTileChanged(object sender, TileChangedEventArgs eventArgs)
        {
            // If this gets hammered you could probably queue up all the tile changes every tick but I doubt that would ever happen.
            var entities = EntityManager.GetEntities(EntityQuery);

            foreach (var entity in entities)
            {
                // If the tile becomes space then delete it (potentially change by design)
                if (eventArgs.NewTile.GridIndex == entity.Transform.GridID &&
                    entity.TryGetComponent(out SnapGridComponent snapGridComponent) &&
                    snapGridComponent.Position == eventArgs.NewTile.GridIndices &&
                    eventArgs.NewTile.Tile.IsEmpty)
                {
                    entity.Delete();
                    break; // Currently it's one puddle per tile, if that changes remove this
                }
            }
        }
    }
}
