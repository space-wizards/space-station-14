using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.IconSmoothing
{
    // TODO: just make this set appearance data?
    /// <summary>
    ///     Entity system implementing the logic for <see cref="IconSmoothComponent"/>
    /// </summary>
    [UsedImplicitly]
    internal sealed class IconSmoothSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<EntityUid> _dirtyEntities = new();
        private readonly Queue<EntityUid> _anchorChangedEntities = new();

        private int _generation;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<IconSmoothComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<IconSmoothComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<IconSmoothComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, IconSmoothComponent component, ComponentStartup args)
        {
            var xform = Transform(uid);
            if (xform.Anchored)
            {
                component.LastPosition = _mapManager.TryGetGrid(xform.GridUid, out var grid)
                    ? (xform.GridUid.Value, grid.TileIndicesFor(xform.Coordinates))
                    : (null, new Vector2i(0, 0));

                DirtyNeighbours(uid, component);
            }

            if (component.Mode != IconSmoothingMode.Corners || !TryComp(uid, out SpriteComponent? sprite))
                return;

            var state0 = $"{component.StateBase}0";
            sprite.LayerMapSet(CornerLayers.SE, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(CornerLayers.SE, DirectionOffset.None);
            sprite.LayerMapSet(CornerLayers.NE, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(CornerLayers.NE, DirectionOffset.CounterClockwise);
            sprite.LayerMapSet(CornerLayers.NW, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(CornerLayers.NW, DirectionOffset.Flip);
            sprite.LayerMapSet(CornerLayers.SW, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(CornerLayers.SW, DirectionOffset.Clockwise);
        }

        private void OnShutdown(EntityUid uid, IconSmoothComponent component, ComponentShutdown args)
        {
            DirtyNeighbours(uid, component);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var xformQuery = GetEntityQuery<TransformComponent>();
            var smoothQuery = GetEntityQuery<IconSmoothComponent>();

            // first process anchor state changes.
            while (_anchorChangedEntities.TryDequeue(out var uid))
            {
                if (!xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                if (xform.MapID == MapId.Nullspace)
                {
                    // in null-space. Almost certainly because it left PVS. If something ever gets sent to null-space
                    // for reasons other than this (or entity deletion), then maybe we still need to update ex-neighbor
                    // smoothing here.
                    continue;
                }

                DirtyNeighbours(uid, comp: null, xform, smoothQuery);
            }

            // Next, update actual sprites.
            if (_dirtyEntities.Count == 0)
                return;

            _generation += 1;

            var spriteQuery = GetEntityQuery<SpriteComponent>();

            // Performance: This could be spread over multiple updates, or made parallel.
            while (_dirtyEntities.TryDequeue(out var uid))
            {
                CalculateNewSprite(uid, spriteQuery, smoothQuery, xformQuery);
            }
        }

        public void DirtyNeighbours(EntityUid uid, IconSmoothComponent? comp = null, TransformComponent? transform = null, EntityQuery<IconSmoothComponent>? smoothQuery = null)
        {
            smoothQuery ??= GetEntityQuery<IconSmoothComponent>();
            if (!smoothQuery.Value.Resolve(uid, ref comp))
                return;

            _dirtyEntities.Enqueue(uid);

            if (!Resolve(uid, ref transform))
                return;

            Vector2i pos;

            if (transform.Anchored && _mapManager.TryGetGrid(transform.GridUid, out var grid))
            {
                pos = grid.CoordinatesToTile(transform.Coordinates);
            }
            else
            {
                // Entity is no longer valid, update around the last position it was at.
                if (comp.LastPosition is not (EntityUid gridId, Vector2i oldPos))
                    return;

                if (!_mapManager.TryGetGrid(gridId, out grid))
                    return;

                pos = oldPos;
            }

            // Yes, we updates ALL smoothing entities surrounding us even if they would never smooth with us.
            DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 0)));
            DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 0)));
            DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, 1)));
            DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, -1)));

            if (comp.Mode is IconSmoothingMode.Corners or IconSmoothingMode.NoSprite or IconSmoothingMode.Diagonal)
            {
                DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 1)));
                DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, -1)));
                DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 1)));
                DirtyEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, -1)));
            }
        }

        private void DirtyEntities(IEnumerable<EntityUid> entities)
        {
            // Instead of doing HasComp -> Enqueue -> TryGetComp, we will just enqueue all entities. Generally when
            // dealing with walls neighboring anchored entities will also be walls, and in those instances that will
            // require one less component fetch/check.
            foreach (var entity in entities)
            {
                _dirtyEntities.Enqueue(entity);
            }
        }

        private void OnAnchorChanged(EntityUid uid, IconSmoothComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Detaching)
                _anchorChangedEntities.Enqueue(uid);
        }

        private void CalculateNewSprite(EntityUid uid,
            EntityQuery<SpriteComponent> spriteQuery,
            EntityQuery<IconSmoothComponent> smoothQuery,
            EntityQuery<TransformComponent> xformQuery,
            IconSmoothComponent? smooth = null)
        {
            // The generation check prevents updating an entity multiple times per tick.
            // As it stands now, it's totally possible for something to get queued twice.
            // Generation on the component is set after an update so we can cull updates that happened this generation.
            if (!smoothQuery.Resolve(uid, ref smooth, false)
                || smooth.Mode == IconSmoothingMode.NoSprite
                || smooth.UpdateGeneration == _generation)
            {
                return;
            }
            smooth.UpdateGeneration = _generation;

            if (!spriteQuery.TryGetComponent(uid, out var sprite))
            {
                Logger.Error($"Encountered a icon-smoothing entity without a sprite: {ToPrettyString(uid)}");
                RemComp(uid, smooth);
                return;
            }

            var xform = xformQuery.GetComponent(uid);

            MapGridComponent? grid = null;

            if (xform.Anchored)
            {
                if (!_mapManager.TryGetGrid(xform.GridUid, out grid))
                {
                    Logger.Error($"Failed to calculate IconSmoothComponent sprite in {uid} because grid {xform.GridUid} was missing.");
                    return;
                }
            }

            switch (smooth.Mode)
            {
                case IconSmoothingMode.Corners:
                    CalculateNewSpriteCorners(grid, smooth, sprite, xform, smoothQuery);
                    break;
                case IconSmoothingMode.CardinalFlags:
                    CalculateNewSpriteCardinal(grid, smooth, sprite, xform, smoothQuery);
                    break;
                case IconSmoothingMode.Diagonal:
                    CalculateNewSpriteDiagonal(grid, smooth, sprite, xform, smoothQuery);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateNewSpriteDiagonal(MapGridComponent? grid, IconSmoothComponent smooth,
            SpriteComponent sprite, TransformComponent xform, EntityQuery<IconSmoothComponent> smoothQuery)
        {
            if (grid == null)
            {
                sprite.LayerSetState(0, $"{smooth.StateBase}0");
                return;
            }

            var neighbors = new Vector2[]
            {
                new(1, 0),
                new(1, -1),
                new(0, -1),
            };

            var pos = grid.TileIndicesFor(xform.Coordinates);
            var rotation = xform.LocalRotation;
            var matching = true;

            for (var i = 0; i < neighbors.Length; i++)
            {
                var neighbor = (Vector2i) rotation.RotateVec(neighbors[i]);
                matching = matching && MatchingEntity(smooth, grid.GetAnchoredEntities(pos + neighbor), smoothQuery);
            }

            if (matching)
            {
                sprite.LayerSetState(0, $"{smooth.StateBase}1");
                return;
            }

            sprite.LayerSetState(0, $"{smooth.StateBase}0");
        }

        private void CalculateNewSpriteCardinal(MapGridComponent? grid, IconSmoothComponent smooth, SpriteComponent sprite, TransformComponent xform, EntityQuery<IconSmoothComponent> smoothQuery)
        {
            var dirs = CardinalConnectDirs.None;

            if (grid == null)
            {
                sprite.LayerSetState(0, $"{smooth.StateBase}{(int) dirs}");
                return;
            }

            var pos = grid.TileIndicesFor(xform.Coordinates);
            if (MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.North)), smoothQuery))
                dirs |= CardinalConnectDirs.North;
            if (MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.South)), smoothQuery))
                dirs |= CardinalConnectDirs.South;
            if (MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.East)), smoothQuery))
                dirs |= CardinalConnectDirs.East;
            if (MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.West)), smoothQuery))
                dirs |= CardinalConnectDirs.West;

            sprite.LayerSetState(0, $"{smooth.StateBase}{(int) dirs}");
        }

        private bool MatchingEntity(IconSmoothComponent smooth, IEnumerable<EntityUid> candidates, EntityQuery<IconSmoothComponent> smoothQuery)
        {
            foreach (var entity in candidates)
            {
                if (smoothQuery.TryGetComponent(entity, out var other) && other.SmoothKey == smooth.SmoothKey)
                    return true;
            }

            return false;
        }

        private void CalculateNewSpriteCorners(MapGridComponent? grid, IconSmoothComponent smooth, SpriteComponent sprite, TransformComponent xform, EntityQuery<IconSmoothComponent> smoothQuery)
        {
            var (cornerNE, cornerNW, cornerSW, cornerSE) = grid == null
                ? (CornerFill.None, CornerFill.None, CornerFill.None, CornerFill.None)
                : CalculateCornerFill(grid, smooth, xform, smoothQuery);

            // TODO figure out a better way to set multiple sprite layers.
            // This will currently re-calculate the sprite bounding box 4 times.
            // It will also result in 4-8 sprite update events being raised when it only needs to be 1-2.
            // At the very least each event currently only queues a sprite for updating.
            // Oh god sprite component is a mess.
            sprite.LayerSetState(CornerLayers.NE, $"{smooth.StateBase}{(int) cornerNE}");
            sprite.LayerSetState(CornerLayers.SE, $"{smooth.StateBase}{(int) cornerSE}");
            sprite.LayerSetState(CornerLayers.SW, $"{smooth.StateBase}{(int) cornerSW}");
            sprite.LayerSetState(CornerLayers.NW, $"{smooth.StateBase}{(int) cornerNW}");
        }

        private (CornerFill ne, CornerFill nw, CornerFill sw, CornerFill se) CalculateCornerFill(MapGridComponent grid, IconSmoothComponent smooth, TransformComponent xform, EntityQuery<IconSmoothComponent> smoothQuery)
        {
            var pos = grid.TileIndicesFor(xform.Coordinates);
            var n = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.North)), smoothQuery);
            var ne = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.NorthEast)), smoothQuery);
            var e = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.East)), smoothQuery);
            var se = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.SouthEast)), smoothQuery);
            var s = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.South)), smoothQuery);
            var sw = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.SouthWest)), smoothQuery);
            var w = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.West)), smoothQuery);
            var nw = MatchingEntity(smooth, grid.GetAnchoredEntities(pos.Offset(Direction.NorthWest)), smoothQuery);

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

        // TODO consider changing this to use DirectionFlags?
        // would require re-labelling all the RSI states.
        [Flags]
        private enum CardinalConnectDirs : byte
        {
            None = 0,
            North = 1,
            South = 2,
            East = 4,
            West = 8
        }


        [Flags]
        private enum CornerFill : byte
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

        private enum CornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
