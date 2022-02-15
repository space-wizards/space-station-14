using Content.Shared.Interaction;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedSubFloorHideSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly TrayScannerSystem _trayScannerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.GridChanged += MapManagerOnGridChanged;
            _mapManager.TileChanged += MapManagerOnTileChanged;

            SubscribeLocalEvent<SubFloorHideComponent, ComponentStartup>(OnSubFloorStarted);
            SubscribeLocalEvent<SubFloorHideComponent, ComponentShutdown>(OnSubFloorTerminating);
            SubscribeLocalEvent<SubFloorHideComponent, AnchorStateChangedEvent>(HandleAnchorChanged);
            SubscribeLocalEvent<SubFloorHideComponent, InteractUsingEvent>(OnInteractionAttempt);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.GridChanged -= MapManagerOnGridChanged;
            _mapManager.TileChanged -= MapManagerOnTileChanged;
        }

        private void OnInteractionAttempt(EntityUid uid, SubFloorHideComponent component, InteractUsingEvent args)
        {
            // TODO make this use an interact attempt event or something. Handling an InteractUsing is not going to work in general.
            args.Handled = component.IsUnderCover;
        }

        private void OnSubFloorStarted(EntityUid uid, SubFloorHideComponent component, ComponentStartup _)
        {
            UpdateFloorCover(uid, component);
            UpdateAppearance(uid, component);
            EntityManager.EnsureComponent<CollideOnAnchorComponent>(uid);
        }

        private void OnSubFloorTerminating(EntityUid uid, SubFloorHideComponent component, ComponentShutdown _)
        {
            // If component is being deleted don't need to worry about updating any component stuff because it won't matter very shortly.
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating)
                return;

            // Regardless of whether we're on a subfloor or not, unhide.
            component.IsUnderCover = false;
            UpdateAppearance(uid, component);
        }

        private void HandleAnchorChanged(EntityUid uid, SubFloorHideComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                var xform = Transform(uid);
                _trayScannerSystem.OnSubfloorAnchored(uid, component, xform);
                UpdateFloorCover(uid, component, xform);

                if (component.IsUnderCover)
                    UpdateAppearance(uid, component);
            }
            else if (component.IsUnderCover)
            {
                component.IsUnderCover = false;
                UpdateAppearance(uid, component);
            }
        }

        private void MapManagerOnTileChanged(object? sender, TileChangedEventArgs e)
        {
            if (e.OldTile.IsEmpty)
                return; // Nothing is anchored here anyways.

            if (e.NewTile.Tile.IsEmpty)
                return; // Anything that was here will be unanchored anyways.

            UpdateTile(_mapManager.GetGrid(e.NewTile.GridIndex), e.NewTile.GridIndices);
        }

        private void MapManagerOnGridChanged(object? sender, GridChangedEventArgs e)
        {
            foreach (var modified in e.Modified)
            {
                UpdateTile(e.Grid, modified.position);
            }
        }

        /// <summary>
        ///     Update whether a given entity is currently covered by a floor tile.
        /// </summary>
        private void UpdateFloorCover(EntityUid uid, SubFloorHideComponent? component = null, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref component, ref xform))
                return;

            if (xform.Anchored && _mapManager.TryGetGrid(xform.GridID, out var grid))
                component.IsUnderCover = HasFloorCover(grid, grid.TileIndicesFor(xform.Coordinates));
            else
                component.IsUnderCover = false;

            UpdateAppearance(uid, component);
        }

        private bool HasFloorCover(IMapGrid grid, Vector2i position)
        {
            // TODO Redo this function. Currently wires on an asteroid are always "below the floor"
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[grid.GetTileRef(position).Tile.TypeId];
            return !tileDef.IsSubFloor;
        }

        private void UpdateTile(IMapGrid grid, Vector2i position)
        {
            var covered = HasFloorCover(grid, position);

            foreach (var uid in grid.GetAnchoredEntities(position))
            {
                if (!TryComp(uid, out SubFloorHideComponent? hideComp))
                    continue;

                if (hideComp.IsUnderCover == covered)
                    continue;

                hideComp.IsUnderCover = covered;
                UpdateAppearance(uid, hideComp);
            }
        }

        /// <summary>
        ///     This function is used by T-Ray scanners or other sub-floor revealing entities to toggle visibility.
        /// </summary>
        public void SetEntitiesRevealed(IEnumerable<EntityUid> entities, EntityUid revealer, bool visible)
        {
            foreach (var uid in entities)
            {
                SetEntityRevealed(uid, revealer, visible);
            }
        }

        /// <summary>
        ///     This function is used by T-Ray scanners or other sub-floor revealing entities to toggle visibility.
        /// </summary>
        public void SetEntityRevealed(EntityUid uid, EntityUid revealer, bool visible, SubFloorHideComponent? hideComp = null)
        {
            if (!Resolve(uid, ref hideComp, false))
                return;

            if (visible)
            {
                if (hideComp.RevealedBy.Add(revealer) && hideComp.RevealedBy.Count == 1)
                    UpdateAppearance(uid, hideComp);

                return;
            }

            if (hideComp.RevealedBy.Remove(revealer) && hideComp.RevealedBy.Count == 0)
                UpdateAppearance(uid, hideComp);
        }

        public void UpdateAppearance(
            EntityUid uid,
            SubFloorHideComponent? hideComp = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref hideComp, ref appearance, false))
                return;

            appearance.SetData(SubFloorVisuals.Covered, hideComp.IsUnderCover);
            appearance.SetData(SubFloorVisuals.ScannerRevealed, hideComp.RevealedBy.Count != 0);
        }
    }

    [Serializable, NetSerializable]
    public enum SubFloorVisuals : byte
    {
        Covered, // is there a floor tile over this entity
        ScannerRevealed, // is this entity revealed by a scanner or some other entity?
    }
}
