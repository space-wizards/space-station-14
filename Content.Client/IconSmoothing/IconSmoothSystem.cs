using Content.Client.Wall.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.IconSmoothing
{
    /// <summary>
    ///     Entity system implementing the logic for <see cref="IconSmoothComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed partial class IconSmoothSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        private ISawmill _sawmill = default!;

        private readonly Queue<EntityUid> _dirtyEntities = new(64);

        private int _generation;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("ismooth");

            SubscribeLocalEvent<IconSmoothComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<IconSmoothComponent, ComponentStartup>(OnSmoothStartup);
            SubscribeLocalEvent<IconSmoothComponent, ComponentShutdown>(OnSmoothShutdown);
            SubscribeLocalEvent<ReinforcedWallComponent, ComponentStartup>(OnReinforcedStartup);
        }

        private void UpdateSmoothPos(EntityUid uid, IconSmoothComponent component)
        {
            if (Transform(uid).Anchored)
            {
                // ensures lastposition initial value is populated on spawn. Just calling
                // the hook here would cause a dirty event to fire needlessly
                UpdateLastPosition(component);
                UpdateSmoothing(uid, component);
            }
        }

        private void OnSmoothStartup(EntityUid uid, IconSmoothComponent component, ComponentStartup args)
        {
            UpdateSmoothPos(uid, component);

            if (TryComp<SpriteComponent>(uid, out var sprite) && component.Mode == IconSmoothingMode.Corners)
            {
                var state0 = $"{component.StateBase}0";
                sprite.LayerMapSet(CornerLayers.SE, sprite.AddLayerState(state0));
                sprite.LayerSetDirOffset(CornerLayers.SE, SpriteComponent.DirectionOffset.None);
                sprite.LayerMapSet(CornerLayers.NE, sprite.AddLayerState(state0));
                sprite.LayerSetDirOffset(CornerLayers.NE, SpriteComponent.DirectionOffset.CounterClockwise);
                sprite.LayerMapSet(CornerLayers.NW, sprite.AddLayerState(state0));
                sprite.LayerSetDirOffset(CornerLayers.NW, SpriteComponent.DirectionOffset.Flip);
                sprite.LayerMapSet(CornerLayers.SW, sprite.AddLayerState(state0));
                sprite.LayerSetDirOffset(CornerLayers.SW, SpriteComponent.DirectionOffset.Clockwise);
            }
        }

        private void OnSmoothShutdown(EntityUid uid, IconSmoothComponent component, ComponentShutdown args)
        {
            UpdateSmoothing(uid, component);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (_dirtyEntities.Count == 0)
                return;

            _generation += 1;

            // Performance: This could be spread over multiple updates, or made parallel.
            while (_dirtyEntities.TryDequeue(out var ent))
            {
                CalculateNewSprite(ent);
            }
        }

        private void UpdateLastPosition(IconSmoothComponent component)
        {
            var transform = Transform(component.Owner);

            if (_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                component.LastPosition = (transform.GridID, grid.TileIndicesFor(transform.Coordinates));
            }
            else
            {
                // When this is called during component startup, the transform can end up being with an invalid grid ID.
                // In that case, use this.
                component.LastPosition = (GridId.Invalid, new Vector2i(0, 0));
            }
        }

        public void UpdateSmoothing(EntityUid uid, IconSmoothComponent? comp = null)
        {
            if (!Resolve(uid, ref comp))
                return;

            _dirtyEntities.Enqueue(uid);

            var transform = Transform(uid);
            Vector2i pos;

            if (transform.Anchored && _mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                pos = grid.CoordinatesToTile(transform.Coordinates);
            }
            else
            {
                // Entity is no longer valid, update around the last position it was at.
                if (comp.LastPosition is not var (gridId, oldPos))
                    return;

                if (!_mapManager.TryGetGrid(gridId, out grid))
                    return;

                pos = oldPos;
            }

            // Yes, we updates ALL smoothing entities surrounding us even if they would never smooth with us.
            // This is simpler to implement. If you want to optimize it be my guest.
            var smoothQuery = GetEntityQuery<IconSmoothComponent>();
            var reinforcedQuery = GetEntityQuery<ReinforcedWallComponent>();

            AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(1, 0)), smoothQuery, reinforcedQuery);
            AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(-1, 0)), smoothQuery, reinforcedQuery);
            AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, 1)), smoothQuery, reinforcedQuery);
            AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, -1)), smoothQuery, reinforcedQuery);

            if (comp.Mode == IconSmoothingMode.Corners)
            {
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(1, 1)), smoothQuery, reinforcedQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(-1, -1)), smoothQuery, reinforcedQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(-1, 1)), smoothQuery, reinforcedQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(1, -1)), smoothQuery, reinforcedQuery);
            }
        }

        private void OnAnchorChanged(EntityUid uid, IconSmoothComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateSmoothing(uid, component);
        }

        private void AddValidEntities(AnchoredEntitiesEnumerator enumerator, EntityQuery<IconSmoothComponent> smoothQuery, EntityQuery<ReinforcedWallComponent> reinforcedQuery)
        {
            while (enumerator.MoveNext(out var ent))
            {
                if (!smoothQuery.HasComponent(ent.Value) &&
                    !reinforcedQuery.HasComponent(ent.Value)) continue;
                _dirtyEntities.Enqueue(ent.Value);
            }
        }

        private void CalculateNewSprite(EntityUid euid)
        {
            // The generation check prevents updating an entity multiple times per tick.
            // As it stands now, it's totally possible for something to get queued twice.
            // Generation on the component is set after an update so we can cull updates that happened this generation.
            if (!EntityManager.EntityExists(euid)
                || !EntityManager.TryGetComponent(euid, out IconSmoothComponent? smoothing)
                || smoothing.UpdateGeneration == _generation)
            {
                return;
            }

            CalculateNewSprite(smoothing);
            smoothing.UpdateGeneration = _generation;
        }

        private void CalculateNewSprite(IconSmoothComponent component)
        {
            var transform = Transform(component.Owner);

            if (!transform.Anchored)
            {
                CalculateNewGridSprite(component, null);
                return;
            }

            if (!_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                _sawmill.Error($"Failed to calculate IconSmoothComponent sprite in {component.Owner} because grid {transform.GridID} was missing.");
                return;
            }

            CalculateNewGridSprite(component, grid);
        }

        private void CalculateNewGridSprite(IconSmoothComponent component, IMapGrid? grid)
        {
            if (component is ReinforcedWallComponent reinforced && TryComp<SpriteComponent>(component.Owner, out var sprite))
            {
                var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill(component, grid);

                sprite.LayerSetState(ReinforcedCornerLayers.NE, $"{reinforced.ReinforcedStateBase}{(int) cornerNE}");
                sprite.LayerSetState(ReinforcedCornerLayers.SE, $"{reinforced.ReinforcedStateBase}{(int) cornerSE}");
                sprite.LayerSetState(ReinforcedCornerLayers.SW, $"{reinforced.ReinforcedStateBase}{(int) cornerSW}");
                sprite.LayerSetState(ReinforcedCornerLayers.NW, $"{reinforced.ReinforcedStateBase}{(int) cornerNW}");
                return;
            }

            switch (component.Mode)
            {
                case IconSmoothingMode.Corners:
                    CalculateNewSpriteCorners(component, grid);
                    break;
                case IconSmoothingMode.CardinalFlags:
                    CalculateNewSpriteCardinal(component, grid);
                    break;
                case IconSmoothingMode.NoSprite:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateNewSpriteCardinal(IconSmoothComponent component, IMapGrid? grid)
        {
            if (!TryComp<SpriteComponent>(component.Owner, out var sprite))
            {
                return;
            }

            var dirs = CardinalConnectDirs.None;

            if (grid == null)
            {
                sprite.LayerSetState(0, $"{component.StateBase}{(int) dirs}");
                return;
            }

            var xform = Transform(component.Owner);
            var gridPos = grid.CoordinatesToTile(xform.Coordinates);
            var smoothQuery = GetEntityQuery<IconSmoothComponent>();
            var reinforcedQuery = GetEntityQuery<ReinforcedWallComponent>();

            if (MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(0, 1)), smoothQuery, reinforcedQuery))
                dirs |= CardinalConnectDirs.North;
            if (MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(0, -1)), smoothQuery, reinforcedQuery))
                dirs |= CardinalConnectDirs.South;
            if (MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(1, 0)), smoothQuery, reinforcedQuery))
                dirs |= CardinalConnectDirs.East;
            if (MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(-1, 0)), smoothQuery, reinforcedQuery))
                dirs |= CardinalConnectDirs.West;

            sprite.LayerSetState(0, $"{component.StateBase}{(int) dirs}");
        }

        private void CalculateNewSpriteCorners(IconSmoothComponent component, IMapGrid? grid)
        {
            if (!TryComp<SpriteComponent>(component.Owner, out var sprite)) return;

            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill(component, grid);

            sprite.LayerSetState(CornerLayers.NE, $"{component.StateBase}{(int) cornerNE}");
            sprite.LayerSetState(CornerLayers.SE, $"{component.StateBase}{(int) cornerSE}");
            sprite.LayerSetState(CornerLayers.SW, $"{component.StateBase}{(int) cornerSW}");
            sprite.LayerSetState(CornerLayers.NW, $"{component.StateBase}{(int) cornerNW}");
        }

        private (CornerFill ne, CornerFill nw, CornerFill sw, CornerFill se) CalculateCornerFill(IconSmoothComponent component, IMapGrid? grid)
        {
            if (grid == null)
            {
                return (CornerFill.None, CornerFill.None, CornerFill.None, CornerFill.None);
            }

            var xform = Transform(component.Owner);
            var gridPos = grid.CoordinatesToTile(xform.Coordinates);
            var smoothQuery = GetEntityQuery<IconSmoothComponent>();
            var reinforcedQuery = GetEntityQuery<ReinforcedWallComponent>();

            var n = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(0, 1)), smoothQuery, reinforcedQuery);
            var ne = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(1, 1)), smoothQuery, reinforcedQuery);
            var e = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(1, 0)), smoothQuery, reinforcedQuery);
            var se = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(1, -1)), smoothQuery, reinforcedQuery);
            var s = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(0, -1)), smoothQuery, reinforcedQuery);
            var sw = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(-1, -1)), smoothQuery, reinforcedQuery);
            var w = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(-1, 0)), smoothQuery, reinforcedQuery);
            var nw = MatchingEntity(component.SmoothKey, grid.GetAnchoredEntitiesEnumerator(gridPos + new Vector2i(-1, 1)), smoothQuery, reinforcedQuery);

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;
            }

            // Local is fine as we already know it's parented to the grid (due to the way anchoring works).
            switch (xform.LocalRotation.GetCardinalDir())
            {
                case Direction.North:
                    return (cornerSW, cornerSE, cornerNE, cornerNW);
                case Direction.West:
                    return (cornerSE, cornerNE, cornerNW, cornerSW);
                case Direction.South:
                    return (cornerNE, cornerNW, cornerSW, cornerSE);
                default:
                    return (cornerNW, cornerSW, cornerSE, cornerNE);
            }
        }

        private bool MatchingEntity(string? smoothKey, AnchoredEntitiesEnumerator enumerator, EntityQuery<IconSmoothComponent> smoothQuery, EntityQuery<ReinforcedWallComponent> reinforcedQuery)
        {
            while (enumerator.MoveNext(out var ent))
            {
                if ((!smoothQuery.TryGetComponent(ent.Value, out var smooth) ||
                    smooth.SmoothKey != smoothKey) &&
                    (!reinforcedQuery.TryGetComponent(ent.Value, out var reinforced) ||
                     reinforced.SmoothKey != smoothKey)) continue;

                return true;
            }

            return false;
        }
    }

    [Flags]
    public enum CardinalConnectDirs : byte
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }

    [Flags]
    public enum CornerFill : byte
    {
        // These values are pulled from Baystation12.
        // I'm too lazy to convert the state names.
        None = 0,

        // The cardinal tile counter-clockwise of this corner is filled.
        CounterClockwise = 1,

        // The diagonal tile in the direction of this corner.
        Diagonal = 2,

        // The cardinal tile clockwise of this corner is filled.
        Clockwise = 4,
    }

    public enum CornerLayers : byte
    {
        SE,
        NE,
        NW,
        SW,
    }
}
