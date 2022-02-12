using Content.Shared.Interaction;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    [UsedImplicitly]
    public sealed class SubFloorHideSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedTrayScannerSystem _trayScannerSystem = default!;

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
            SubscribeLocalEvent<SubFloorHideComponent, InteractUsingEvent>(OnInteractionAttempt);
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
            Dirty(subFloor);
            UpdateAppearance(subFloor.Owner);
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

        private void HandleComponentState(EntityUid uid, SubFloorHideComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SubFloorHideComponentState state)
                return;

            component.Enabled = state.Enabled;
            UpdateAppearance(uid, component);
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

            // Update normally.
            UpdateAppearance(uid, component);
        }

        private bool HasFloorCover(IMapGrid grid, Vector2i position)
        {
            // TODO Redo this function. Currently wires on an asteroid are always "below the floor"
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[grid.GetTileRef(position).Tile.TypeId];
            return !tileDef.IsSubFloor;
        }

        private void UpdateAll()
        {
            foreach (var comp in EntityManager.EntityQuery<SubFloorHideComponent>(true))
            {
                UpdateAppearance(comp.Owner, comp);
            }
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
        public void SetEntitiesRevealed(IEnumerable<EntityUid> entities, EntityUid revealer, bool visible, IEnumerable<object>? appearanceKeys = null)
        {
            foreach (var uid in entities)
            {
                SetEntityRevealed(uid, revealer, visible);
            }
        }

        /// <summary>
        ///     This function is used by T-Ray scanners or other sub-floor revealing entities to toggle visibility.
        /// </summary>
        public void SetEntityRevealed(EntityUid uid, EntityUid revealer, bool visible,
            SubFloorHideComponent? hideComp = null,
            IEnumerable<object>? appearanceKeys = null)
        {
            if (!Resolve(uid, ref hideComp))
                return;

            if (visible)
            {
                if (hideComp.RevealedBy.Add(revealer) && hideComp.RevealedBy.Count == 1)
                    UpdateAppearance(uid, hideComp, appearanceKeys);

                return;
            }

            if (hideComp.RevealedBy.Remove(revealer) && hideComp.RevealedBy.Count == 0)
                UpdateAppearance(uid, hideComp, appearanceKeys);
        }

        public void UpdateAppearance(EntityUid uid, SubFloorHideComponent? hideComp = null, IEnumerable<object>? appearanceKeys = null)
        {
            if (!Resolve(uid, ref hideComp))
                return;

            var revealedWithoutEntity = ShowAll || !hideComp.IsUnderCover;
            var revealed = revealedWithoutEntity || hideComp.RevealedBy.Count != 0;

            // if there are no keys given,
            // or if the subfloor is already revealed,
            // set the keys to the default:
            //
            // the reason why it's set to default when the subfloor is
            // revealed without an entity is because the appearance keys
            // should only apply if the visualizer is underneath a subfloor
            if (appearanceKeys == null || revealedWithoutEntity) appearanceKeys = _defaultVisualizerKeys;

            ShowSubfloorSprite(uid, revealed, appearanceKeys);
        }

        private void ShowSubfloorSprite(EntityUid uid, bool revealed, IEnumerable<object> appearanceKeys)
        {
            // Show sprite
            if (EntityManager.TryGetComponent(uid, out SharedSpriteComponent? spriteComponent))
            {
                spriteComponent.Visible = revealed;
            }

            // Set an appearance data value so visualizers can use this as needed.
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
            {
                foreach (var key in appearanceKeys)
                {
                    switch (key)
                    {
                        case Enum enumKey:
                            appearanceComponent.SetData(enumKey, revealed);
                            break;
                        case string stringKey:
                            appearanceComponent.SetData(stringKey, revealed);
                            break;
                    }
                }
            }
        }

        private static List<object> _defaultVisualizerKeys = new List<object>{ SubFloorVisuals.SubFloor };
    }

    [Serializable, NetSerializable]
    public enum SubFloorVisuals : byte
    {
        SubFloor,
    }
}
