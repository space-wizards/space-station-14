using Content.Client.GameObjects.Components;
using Content.Shared.Maps;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

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

        private void HandleDirtyEvent(SubFloorHideDirtyEvent ev)
        {
            var grid = _mapManager.GetGrid(ev.Sender.Transform.GridID);
            var indices = grid.WorldToTile(ev.Sender.Transform.WorldPosition);
            UpdateTile(grid, indices);
        }

        private void MapManagerOnTileChanged(object sender, TileChangedEventArgs e)
        {
            UpdateTile(_mapManager.GetGrid(e.NewTile.GridIndex), e.NewTile.GridIndices);
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
            var tile = grid.GetTileRef(position);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
            foreach (var snapGridComponent in grid.GetSnapGridCell(position, SnapGridOffset.Center))
            {
                var entity = snapGridComponent.Owner;
                if (!entity.TryGetComponent(out SubFloorHideComponent subFloorComponent) ||
                    !entity.TryGetComponent(out ISpriteComponent spriteComponent))
                {
                    continue;
                }

                spriteComponent.Visible = !subFloorComponent.Running || tileDef.IsSubFloor;
            }
        }
    }
}
