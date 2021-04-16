#nullable enable
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.EntitySystems
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
            foreach (var comp in EntityManager.ComponentManager.EntityQuery<SubFloorHideComponent>(true))
            {
                if (!_mapManager.TryGetGrid(comp.Owner.Transform.GridID, out var grid)) return;

                var snapPos = comp.Owner.GetComponent<SnapGridComponent>();
                UpdateTile(grid, snapPos.Position);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            _mapManager.GridChanged += MapManagerOnGridChanged;
            _mapManager.TileChanged += MapManagerOnTileChanged;

            // TODO: Make this sane when EntityStarted becomes a directed event.
            EntityManager.EntityStarted += OnEntityStarted;

            SubscribeLocalEvent<SubFloorHideComponent, EntityTerminatingEvent>(OnSubFloorTerminating);
            SubscribeLocalEvent<SubFloorHideComponent, SnapGridPositionChangedEvent>(OnSnapGridPositionChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.GridChanged -= MapManagerOnGridChanged;
            _mapManager.TileChanged -= MapManagerOnTileChanged;

            EntityManager.EntityStarted -= OnEntityStarted;

            UnsubscribeLocalEvent<SubFloorHideComponent, EntityTerminatingEvent>(OnSubFloorTerminating);
            UnsubscribeLocalEvent<SubFloorHideComponent, SnapGridPositionChangedEvent>(OnSnapGridPositionChanged);
        }

        private void OnEntityStarted(object? sender, EntityUid uid)
        {
            if (ComponentManager.HasComponent<SubFloorHideComponent>(uid))
            {
                UpdateEntity(uid);
            }
        }

        private void OnSubFloorTerminating(EntityUid uid, SubFloorHideComponent component, EntityTerminatingEvent args)
        {
            UpdateEntity(uid);
        }

        private void OnSnapGridPositionChanged(EntityUid uid, SubFloorHideComponent component, SnapGridPositionChangedEvent ev)
        {
            // We do this directly instead of calling UpdateEntity.
            if(_mapManager.TryGetGrid(ev.NewGrid, out var grid))
                UpdateTile(grid, ev.Position);
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
            foreach (var snapGridComponent in grid.GetSnapGridCell(position, SnapGridOffset.Center))
            {
                var entity = snapGridComponent.Owner;
                if (!entity.TryGetComponent(out SubFloorHideComponent? subFloorComponent))
                {
                    continue;
                }

                // Show sprite
                if (entity.TryGetComponent(out SharedSpriteComponent? spriteComponent))
                {
                    spriteComponent.Visible = ShowAll || !subFloorComponent.Running || tileDef.IsSubFloor;
                }

                // So for collision all we care about is that the component is running.
                if (entity.TryGetComponent(out PhysicsComponent? physicsComponent))
                {
                    physicsComponent.CanCollide = !subFloorComponent.Running;
                }
            }
        }
    }
}
