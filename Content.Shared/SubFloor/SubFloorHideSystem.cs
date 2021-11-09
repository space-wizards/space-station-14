using System;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
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
            SubscribeLocalEvent<SubFloorHideComponent, ComponentHandleState>(HandleComponentState);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.GridChanged -= MapManagerOnGridChanged;
            _mapManager.TileChanged -= MapManagerOnTileChanged;
        }

        public void SetEnabled(SubFloorHideComponent subFloor, bool enabled)
        {
            subFloor.Enabled = enabled;
            subFloor.Dirty();
            UpdateEntity(subFloor.Owner.Uid);
        }

        public void SetRequireAnchoring(SubFloorHideComponent subFloor, bool requireAnchored)
        {
            subFloor.RequireAnchored = requireAnchored;
            subFloor.Dirty();
            UpdateEntity(subFloor.Owner.Uid);
        }

        private void OnSubFloorStarted(EntityUid uid, SubFloorHideComponent component, ComponentStartup _)
        {
            UpdateEntity(uid);
            EntityManager.EnsureComponent<CollideOnAnchorComponent>(uid);
        }

        private void OnSubFloorTerminating(EntityUid uid, SubFloorHideComponent component, ComponentShutdown _)
        {
            // If component is being deleted don't need to worry about updating any component stuff because it won't matter very shortly.
            if (EntityManager.GetEntity(uid).LifeStage >= EntityLifeStage.Terminating) return;

            // Regardless of whether we're on a subfloor or not, unhide.
            UpdateEntity(uid, true);
            EntityManager.RemoveComponent<CollideOnAnchorComponent>(uid);
        }

        private void HandleAnchorChanged(EntityUid uid, SubFloorHideComponent component, ref AnchorStateChangedEvent args)
        {
            // We do this directly instead of calling UpdateEntity.
            UpdateEntity(uid);
        }

        private void HandleComponentState(EntityUid uid, SubFloorHideComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SubFloorHideComponentState state)
                return;

            component.Enabled = state.Enabled;
            component.RequireAnchored = state.RequireAnchored;
            UpdateEntity(uid);
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
            foreach (var comp in EntityManager.EntityQuery<SubFloorHideComponent>(true))
            {
                UpdateEntity(comp.Owner.Uid);
            }
        }

        private void UpdateTile(IMapGrid grid, Vector2i position)
        {
            var isSubFloor = IsSubFloor(grid, position);

            foreach (var uid in grid.GetAnchoredEntities(position))
            {
                if(EntityManager.HasComponent<SubFloorHideComponent>(uid))
                    UpdateEntity(uid, isSubFloor);
            }
        }

        private void UpdateEntity(EntityUid uid)
        {
            var transform = EntityManager.GetComponent<TransformComponent>(uid);

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

            // We only need to query the subfloor component to check if it's enabled or not when we're not on subfloor.
            // Getting components is expensive, after all.
            if (!subFloor && EntityManager.TryGetComponent(uid, out SubFloorHideComponent? subFloorHideComponent))
            {
                // If the component isn't enabled, then subfloor will always be true, and the entity will be shown.
                if (!subFloorHideComponent.Enabled)
                {
                    subFloor = true;
                }
                // We only need to query the TransformComp if the SubfloorHide is enabled and requires anchoring.
                else if (subFloorHideComponent.RequireAnchored && EntityManager.TryGetComponent(uid, out TransformComponent? transformComponent))
                {
                    // If we require the entity to be anchored but it's not, this will set subfloor to true, unhiding it.
                    subFloor = !transformComponent.Anchored;
                }
            }

            // Whether to show this entity as visible, visually.
            var subFloorVisible = ShowAll || subFloor;

            // Show sprite
            if (EntityManager.TryGetComponent(uid, out SharedSpriteComponent? spriteComponent))
            {
                spriteComponent.Visible = subFloorVisible;
            }

            // Set an appearance data value so visualizers can use this as needed.
            if (EntityManager.TryGetComponent(uid, out SharedAppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(SubFloorVisuals.SubFloor, subFloorVisible);
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

    [Serializable, NetSerializable]
    public enum SubFloorVisuals : byte
    {
        SubFloor,
    }
}
