using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client.Sprite;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Client.Clickable;

/// <summary>
/// Handles click detection for sprites.
/// </summary>
public sealed partial class ClickableSystem : EntitySystem
{
    [Dependency] private IClickMapManager _clickMapManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IParallelManager _parallel = default!;
    [Dependency] private SharedTransformSystem _transforms = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private SpriteTreeSystem _spriteTree = default!;

    [Dependency] private EntityQuery<ClickableComponent> _clickableQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<FadingSpriteComponent> _fadingSpriteQuery = default!;

    private readonly ClickableEntityComparer _comparer = new();
    private readonly HashSet<Entity<SpriteComponent, TransformComponent>> _queryResults = new();
    private readonly List<ClickableCandidate> _candidates = new();
    private readonly List<(EntityUid Uid, int Depth, uint RenderOrder, float Bottom)> _foundEntities = new();
    private readonly List<EntityUid> _cachedClickableEntities = new();
    private readonly List<ClickableResult> _clickResults = new();
    private ClickableCheckJob _clickableCheckJob = default!;
    private MapCoordinates _cachedCoordinates;
    private IEye? _cachedEye;
    private bool _cachedExcludeFaded;
    private uint _cachedFrame;
    private bool _cacheValid;

    public override void Initialize()
    {
        base.Initialize();
        _clickableCheckJob = new ClickableCheckJob(this, _candidates, _clickResults);
    }

    public EntityUid? GetClickedEntity(MapCoordinates coordinates, IEye? eye)
    {
        var entities = GetClickableEntities(coordinates, eye);
        return entities.Count == 0 ? null : entities[0];
    }

    public IReadOnlyList<EntityUid> GetClickableEntities(MapCoordinates coordinates, IEye? eye, bool excludeFaded = true)
    {
        /*
         * TODO:
         * 1. Stuff like MeleeWeaponSystem need an easy way to hook into viewport specific entities / entities under mouse
         * 2. Cleanup the mess around InteractionOutlineSystem + below the keybind click detection.
         */

        if (eye == null)
            return Array.Empty<EntityUid>();

        if (_cacheValid &&
            _cachedFrame == _timing.CurFrame &&
            _cachedEye == eye &&
            _cachedExcludeFaded == excludeFaded &&
            _cachedCoordinates.Equals(coordinates))
        {
            return _cachedClickableEntities;
        }

        // Find all the entities intersecting our click.
        _queryResults.Clear();
        _spriteTree.QueryAabb(
            _queryResults,
            coordinates.MapId,
            Box2.CenteredAround(coordinates.Position, new Vector2(1, 1)));

        _candidates.Clear();
        _foundEntities.Clear();
        _cachedClickableEntities.Clear();
        _clickResults.Clear();
        foreach (var entity in _queryResults)
        {
            _candidates.Add(new ClickableCandidate(entity.Owner, entity.Comp1, entity.Comp2));
            _clickResults.Add(default);
        }

        _clickableCheckJob.WorldPos = coordinates.Position;
        _clickableCheckJob.Eye = eye;
        _clickableCheckJob.ExcludeFaded = excludeFaded;
        _parallel.ProcessNow(_clickableCheckJob, _candidates.Count);

        for (var i = 0; i < _candidates.Count; i++)
        {
            var result = _clickResults[i];
            if (!result.Clicked)
                continue;

            _foundEntities.Add((result.Uid, result.Depth, result.RenderOrder, result.Bottom));
        }

        if (_foundEntities.Count != 0)
        {
            // Do drawdepth & y-sorting. First index is the top-most sprite (opposite of normal render order).
            _foundEntities.Sort(_comparer);

            foreach (var entity in _foundEntities)
            {
                _cachedClickableEntities.Add(entity.Uid);
            }
        }

        _cachedCoordinates = coordinates;
        _cachedEye = eye;
        _cachedExcludeFaded = excludeFaded;
        _cachedFrame = _timing.CurFrame;
        _cacheValid = true;

        return _cachedClickableEntities;
    }

    /// <summary>
    /// Used to check whether a click worked. Will first check if the click falls inside of some explicit bounding
    /// boxes (see <see cref="Bounds"/>). If that fails, attempts to use automatically generated click maps.
    /// </summary>
    /// <param name="worldPos">The world position that was clicked.</param>
    /// <param name="drawDepth">
    /// The draw depth for the sprite that captured the click.
    /// </param>
    /// <returns>True if the click worked, false otherwise.</returns>
    public bool CheckClick(Entity<ClickableComponent?, SpriteComponent, TransformComponent?, FadingSpriteComponent?> entity, Vector2 worldPos, IEye eye, bool excludeFaded, out int drawDepth, out uint renderOrder, out float bottom)
    {
        if (!_clickableQuery.Resolve(entity.Owner, ref entity.Comp1, false))
        {
            drawDepth = default;
            renderOrder = default;
            bottom = default;
            return false;
        }

        if (!_xformQuery.Resolve(entity.Owner, ref entity.Comp3))
        {
            drawDepth = default;
            renderOrder = default;
            bottom = default;
            return false;
        }

        if (excludeFaded && _fadingSpriteQuery.Resolve(entity.Owner, ref entity.Comp4, false))
        {
            drawDepth = default;
            renderOrder = default;
            bottom = default;
            return false;
        }

        var sprite = entity.Comp2;
        var transform = entity.Comp3;

        if (!sprite.Visible)
        {
            drawDepth = default;
            renderOrder = default;
            bottom = default;
            return false;
        }

        drawDepth = sprite.DrawDepth;
        renderOrder = sprite.RenderOrder;
        var (spritePos, spriteRot) = _transforms.GetWorldPositionRotation(transform);
        var spriteBB = _sprites.CalculateBounds((entity.Owner, sprite), spritePos, spriteRot, eye.Rotation);
        bottom = Matrix3Helpers.CreateRotation(eye.Rotation).TransformBox(spriteBB).Bottom;

        Matrix3x2.Invert(sprite.LocalMatrix, out var invSpriteMatrix);

        // This should have been the rotation of the sprite relative to the screen, but this is not the case with no-rot or directional sprites.
        var relativeRotation = (spriteRot + eye.Rotation).Reduced().FlipPositive();

        var cardinalSnapping = sprite.SnapCardinals ? relativeRotation.GetCardinalDir().ToAngle() : Angle.Zero;

        // First we get `localPos`, the clicked location in the sprite-coordinate frame.
        var entityXform = Matrix3Helpers.CreateInverseTransform(spritePos, sprite.NoRotation ? -eye.Rotation : spriteRot - cardinalSnapping);
        var localPos = Vector2.Transform(Vector2.Transform(worldPos, entityXform), invSpriteMatrix);

        // Check explicitly defined click-able bounds
        if (CheckDirBound((entity.Owner, entity.Comp1, entity.Comp2), relativeRotation, localPos))
            return true;

        // Next check each individual sprite layer using automatically computed click maps.
        var layers = sprite.LayerData;
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (!_sprites.IsVisible(layer))
            {
                continue;
            }

            // Check the layer's texture, if it has one
            if (layer.Texture != null)
            {
                // Convert to image coordinates
                var imagePos = (Vector2i)(localPos * EyeManager.PixelsPerMeter * new Vector2(1, -1) + layer.Texture.Size / 2f);

                if (_clickMapManager.IsOccluding(layer.Texture, imagePos))
                    return true;
            }

            // Either we weren't clicking on the texture, or there wasn't one. In which case: check the RSI next
            if (layer.ActualRsi is not { } rsi || !rsi.TryGetState(layer.State, out var rsiState))
                continue;

            var dir = SpriteComponent.Layer.GetDirection(rsiState.RsiDirections, relativeRotation);

            // convert to layer-local coordinates
            layer.GetLayerDrawMatrix(dir, out var matrix);
            Matrix3x2.Invert(matrix, out var inverseMatrix);
            var layerLocal = Vector2.Transform(localPos, inverseMatrix);

            // Convert to image coordinates
            var layerImagePos = (Vector2i)(layerLocal * EyeManager.PixelsPerMeter * new Vector2(1, -1) + rsiState.Size / 2f);

            // Next, to get the right click map we need the "direction" of this layer that is actually being used to draw the sprite on the screen.
            // This **can** differ from the dir defined before, but can also just be the same.
            if (sprite.EnableDirectionOverride)
                dir = sprite.DirectionOverride.Convert(rsiState.RsiDirections);
            dir = dir.OffsetRsiDir(layer.DirOffset);

            if (_clickMapManager.IsOccluding(layer.ActualRsi!, layer.State, dir, layer.AnimationFrame, layerImagePos))
                return true;
        }

        return false;
    }

    public bool CheckDirBound(Entity<ClickableComponent, SpriteComponent> entity, Angle relativeRotation, Vector2 localPos)
    {
        var clickable = entity.Comp1;
        var sprite = entity.Comp2;

        if (clickable.Bounds == null)
            return false;

        // These explicit bounds only work for either 1 or 4 directional sprites.

        // This would be the orientation of a 4-directional sprite.
        var direction = relativeRotation.GetCardinalDir();

        var modLocalPos = sprite.NoRotation
            ? localPos
            : direction.ToAngle().RotateVec(localPos);

        // First, check the bounding box that is valid for all orientations
        if (clickable.Bounds.All.Contains(modLocalPos))
            return true;

        // Next, get and check the appropriate bounding box for the current sprite orientation
        var boundsForDir = (sprite.EnableDirectionOverride ? sprite.DirectionOverride : direction) switch
        {
            Direction.East => clickable.Bounds.East,
            Direction.North => clickable.Bounds.North,
            Direction.South => clickable.Bounds.South,
            Direction.West => clickable.Bounds.West,
            _ => throw new InvalidOperationException()
        };

        return boundsForDir.Contains(modLocalPos);
    }

    private readonly record struct ClickableCandidate(EntityUid Uid, SpriteComponent Sprite, TransformComponent Transform);

    private struct ClickableResult
    {
        public bool Clicked;
        public EntityUid Uid;
        public int Depth;
        public uint RenderOrder;
        public float Bottom;
    }

    private sealed class ClickableCheckJob : IParallelRobustJob
    {
        private readonly List<ClickableCandidate> _candidates;
        private readonly List<ClickableResult> _results;
        private readonly ClickableSystem _clickable;
        public Vector2 WorldPos;
        public IEye Eye = default!;
        public bool ExcludeFaded;

        public ClickableCheckJob(
            ClickableSystem clickable,
            List<ClickableCandidate> candidates,
            List<ClickableResult> results)
        {
            _clickable = clickable;
            _candidates = candidates;
            _results = results;
        }

        public int BatchSize => 16;

        public void Execute(int index)
        {
            var candidate = _candidates[index];
            ref var result = ref CollectionsMarshal.AsSpan(_results)[index];

            if (!_clickable.CheckClick(
                    (candidate.Uid, null, candidate.Sprite, candidate.Transform),
                    WorldPos,
                    Eye,
                    ExcludeFaded,
                    out result.Depth,
                    out result.RenderOrder,
                    out result.Bottom))
            {
                return;
            }

            result.Clicked = true;
            result.Uid = candidate.Uid;
        }
    }

    private sealed class ClickableEntityComparer : IComparer<(EntityUid Uid, int Depth, uint RenderOrder, float Bottom)>
    {
        public int Compare(
            (EntityUid Uid, int Depth, uint RenderOrder, float Bottom) x,
            (EntityUid Uid, int Depth, uint RenderOrder, float Bottom) y)
        {
            var cmp = y.Depth.CompareTo(x.Depth);
            if (cmp != 0)
                return cmp;

            cmp = y.RenderOrder.CompareTo(x.RenderOrder);
            if (cmp != 0)
                return cmp;

            cmp = -y.Bottom.CompareTo(x.Bottom);
            if (cmp != 0)
                return cmp;

            return y.Uid.CompareTo(x.Uid);
        }
    }
}
