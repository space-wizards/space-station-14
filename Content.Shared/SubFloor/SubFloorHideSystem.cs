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

        public override void Initialize()
        {
            base.Initialize();

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
            // Regardless of whether we're on a subfloor or not, unhide.
            UpdateEntity(uid, true);
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

        private bool IsSubFloor(IMapGrid grid, Vector2i position)
        {
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[grid.GetTileRef(position).Tile.TypeId];
            return tileDef.IsSubFloor;
        }

        private void UpdateAll()
        {
            foreach (var comp in ComponentManager.EntityQuery<SubFloorHideComponent>(true))
            {
                UpdateEntity(comp.Owner.Uid);
            }
        }

        private void UpdateTile(IMapGrid grid, Vector2i position)
        {
            var isSubFloor = IsSubFloor(grid, position);

            foreach (var uid in grid.GetAnchoredEntities(position))
            {
                if(ComponentManager.HasComponent<SubFloorHideComponent>(uid))
                    UpdateEntity(uid, isSubFloor);
            }
        }

        private void UpdateEntity(EntityUid uid)
        {
            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);
            if (!_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                // Not being on a grid counts as no subfloor, unhide this.
                UpdateEntity(uid, true);
                return;
            }

            // Update normally.
            UpdateEntity(uid, IsSubFloor(grid, grid.TileIndicesFor(transform.Coordinates)));
        }

        private void UpdateEntity(EntityUid uid, bool subFloor)
        {
            // We raise an event to allow other entity systems to handle this.
            var subFloorHideEvent = new SubFloorHideEvent(subFloor);
            RaiseLocalEvent(uid, subFloorHideEvent, false);

            // Check if it has been handled by someone else.
            if (subFloorHideEvent.Handled)
                return;

            // Show sprite
            if (ComponentManager.TryGetComponent(uid, out SharedSpriteComponent? spriteComponent))
            {
                spriteComponent.Visible = ShowAll || subFloor;
            }

            // So for collision all we care about is that the component is running.
            if (ComponentManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                physicsComponent.CanCollide = subFloor;
            }
        }
    }

    public class SubFloorHideEvent : HandledEntityEventArgs
    {
        public bool SubFloor { get; }

        public SubFloorHideEvent(bool subFloor)
        {
            SubFloor = subFloor;
        }
    }
}
