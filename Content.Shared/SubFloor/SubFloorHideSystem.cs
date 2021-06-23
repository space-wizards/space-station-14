#nullable enable
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Shared.SubFloor
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    [UsedImplicitly]
    public class SubFloorHideSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private bool _showAll;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowAll
        {
            get => _showAll;
            set
            {
                if (_showAll == value) return;
                _showAll = value;

                UpdateAll();
            }
        }

        private void UpdateAll()
        {
            foreach (var comp in ComponentManager.EntityQuery<SubFloorHideComponent>(true))
            {
                var transform = comp.Owner.Transform;
                if (!_mapManager.TryGetGrid(transform.GridID, out var grid)) return;
                UpdateTile(grid, grid.TileIndicesFor(transform.Coordinates));
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            _mapManager.GridChanged += MapManagerOnGridChanged;
            _mapManager.TileChanged += MapManagerOnTileChanged;

            SubscribeLocalEvent<SubFloorHideComponent, ComponentStartup>(OnSubFloorStarted);
            SubscribeLocalEvent<SubFloorHideComponent, ComponentShutdown>(OnSubFloorTerminating);
            SubscribeLocalEvent<SubFloorHideComponent, AnchorStateChangedEvent>(HandleAnchorChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.GridChanged -= MapManagerOnGridChanged;
            _mapManager.TileChanged -= MapManagerOnTileChanged;
        }

        private void OnSubFloorStarted(EntityUid uid, SubFloorHideComponent component, ComponentStartup _)
        {
            UpdateEntity(uid);
        }

        private void OnSubFloorTerminating(EntityUid uid, SubFloorHideComponent component, ComponentShutdown _)
        {
            UpdateEntity(uid);
        }

        private void HandleAnchorChanged(EntityUid uid, SubFloorHideComponent component, AnchorStateChangedEvent args)
        {
            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);

            // We do this directly instead of calling UpdateEntity.
            if(_mapManager.TryGetGrid(transform.GridID, out var grid))
                UpdateTile(grid, grid.TileIndicesFor(transform.Coordinates));
        }

        private void MapManagerOnTileChanged(object? sender, TileChangedEventArgs e)
        {
            UpdateTile(_mapManager.GetGrid(e.NewTile.GridIndex), e.NewTile.GridIndices);
        }

        private void MapManagerOnGridChanged(object? sender, GridChangedEventArgs e)
        {
            foreach (var modified in e.Modified)
            {
                UpdateTile(e.Grid, modified.position);
            }
        }

        private void UpdateEntity(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out ITransformComponent? transform) ||
                !_mapManager.TryGetGrid(transform.GridID, out var grid)) return;

            UpdateTile(grid, grid.WorldToTile(transform.WorldPosition));
        }

        private void UpdateTile(IMapGrid grid, Vector2i position)
        {
            var tile = grid.GetTileRef(position);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
            foreach (var anchored in grid.GetAnchoredEntities(position))
            {
                if (!ComponentManager.TryGetComponent(anchored, out SubFloorHideComponent? subFloorComponent))
                {
                    continue;
                }

                // Show sprite
                if (ComponentManager.TryGetComponent(anchored, out SharedSpriteComponent ? spriteComponent))
                {
                    spriteComponent.Visible = ShowAll || !subFloorComponent.Running || tileDef.IsSubFloor;
                }

                // So for collision all we care about is that the component is running.
                if (ComponentManager.TryGetComponent(anchored, out PhysicsComponent ? physicsComponent))
                {
                    physicsComponent.CanCollide = !subFloorComponent.Running;
                }
            }
        }
    }
}
