using Content.Client.GameObjects.Components;
using Content.Shared.Maps;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    internal sealed class SubFloorHideSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            _mapManager.GridChanged += MapManagerOnGridChanged;
            _mapManager.TileChanged += MapManagerOnTileChanged;

            SubscribeEvent<SubFloorHideDirtyEvent>(HandleDirtyEvent);
        }

        private void HandleDirtyEvent(object sender, SubFloorHideDirtyEvent ev)
        {
            if (!(sender is IEntity senderEnt))
            {
                return;
            }

            var sprite = senderEnt.GetComponent<ISpriteComponent>();
            var grid = _mapManager.GetGrid(senderEnt.Transform.GridID);
            var position = senderEnt.Transform.GridPosition;
            var tileRef = grid.GetTile(position);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TileId];
            sprite.Visible = tileDef.IsSubFloor;
        }

        private void MapManagerOnTileChanged(object sender, TileChangedEventArgs e)
        {
            UpdateTile(_mapManager.GetGrid(e.NewTile.GridIndex), e.NewTile.GridTile);
        }

        private void MapManagerOnGridChanged(object sender, GridChangedEventArgs e)
        {
            foreach (var modified in e.Modified)
            {
                UpdateTile(e.Grid, modified.position);
            }
        }

        private void UpdateTile(IMapGrid grid, MapIndices position)
        {
            var tile = grid.GetTile(position);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TileId];
            foreach (var snapGridComponent in grid.GetSnapGridCell(position, SnapGridOffset.Center))
            {
                var entity = snapGridComponent.Owner;
                if (!entity.HasComponent<SubFloorHideComponent>() ||
                    !entity.TryGetComponent(out ISpriteComponent spriteComponent))
                {
                    continue;
                }

                spriteComponent.Visible = tileDef.IsSubFloor;
            }
        }
    }
}
