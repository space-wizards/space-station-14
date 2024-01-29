using Content.Shared.Audio;
using Content.Shared.Explosion;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedSubFloorHideSystem : EntitySystem
    {
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
            SubscribeLocalEvent<SubFloorHideComponent, ComponentStartup>(OnSubFloorStarted);
            SubscribeLocalEvent<SubFloorHideComponent, ComponentShutdown>(OnSubFloorTerminating);
            // Like 80% sure this doesn't need to handle re-anchoring.
            SubscribeLocalEvent<SubFloorHideComponent, AnchorStateChangedEvent>(HandleAnchorChanged);
            SubscribeLocalEvent<SubFloorHideComponent, GettingInteractedWithAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<SubFloorHideComponent, GettingAttackedAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<SubFloorHideComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        }

        private void OnGetExplosionResistance(EntityUid uid, SubFloorHideComponent component, ref GetExplosionResistanceEvent args)
        {
            if (component.BlockInteractions && component.IsUnderCover)
                args.DamageCoefficient = 0;
        }

        private void OnAttackAttempt(EntityUid uid, SubFloorHideComponent component, ref GettingAttackedAttemptEvent args)
        {
            if (component.BlockInteractions && component.IsUnderCover)
                args.Cancelled = true;
        }

        private void OnInteractionAttempt(EntityUid uid, SubFloorHideComponent component, GettingInteractedWithAttemptEvent args)
        {
            // No interactions with entities hidden under floor tiles.
            if (component.BlockInteractions && component.IsUnderCover)
                args.Cancel();
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
                UpdateFloorCover(uid, component, xform);
            }
            else if (component.IsUnderCover)
            {
                component.IsUnderCover = false;
                UpdateAppearance(uid, component);
            }
        }

        private void OnTileChanged(ref TileChangedEvent args)
        {
            if (args.OldTile.IsEmpty)
                return; // Nothing is anchored here anyways.

            if (args.NewTile.Tile.IsEmpty)
                return; // Anything that was here will be unanchored anyways.

            UpdateTile(MapManager.GetGrid(args.NewTile.GridUid), args.NewTile.GridIndices);
        }

        /// <summary>
        ///     Update whether a given entity is currently covered by a floor tile.
        /// </summary>
        private void UpdateFloorCover(EntityUid uid, SubFloorHideComponent? component = null, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref component, ref xform))
                return;

            if (xform.Anchored && MapManager.TryGetGrid(xform.GridUid, out var grid))
                component.IsUnderCover = HasFloorCover(grid, grid.TileIndicesFor(xform.Coordinates));
            else
                component.IsUnderCover = false;

            UpdateAppearance(uid, component);
        }

        public bool HasFloorCover(MapGridComponent grid, Vector2i position)
        {
            // TODO Redo this function. Currently wires on an asteroid are always "below the floor"
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[grid.GetTileRef(position).Tile.TypeId];
            return !tileDef.IsSubFloor;
        }

        private void UpdateTile(MapGridComponent grid, Vector2i position)
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

        public void UpdateAppearance(
            EntityUid uid,
            SubFloorHideComponent? hideComp = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref hideComp, false))
                return;

            if (hideComp.BlockAmbience && hideComp.IsUnderCover)
                _ambientSoundSystem.SetAmbience(uid, false);
            else if (hideComp.BlockAmbience && !hideComp.IsUnderCover)
                _ambientSoundSystem.SetAmbience(uid, true);

            if (Resolve(uid, ref appearance, false))
            {
                Appearance.SetData(uid, SubFloorVisuals.Covered, hideComp.IsUnderCover, appearance);
            }
        }
    }

    [Serializable, NetSerializable]
    public enum SubFloorVisuals : byte
    {
        /// <summary>
        /// Is there a floor tile over this entity
        /// </summary>
        Covered,

        /// <summary>
        /// Is this entity revealed by a scanner or some other entity?
        /// </summary>
        ScannerRevealed,
    }

    public enum SubfloorLayers : byte
    {
        FirstLayer
    }
}
