using System.Numerics;
using Content.Client.Pinpointer.UI;
using Content.Client.Graphics;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawShader = "StencilDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();
    private readonly NavMapControl _navMap = new();

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly Dictionary<Color, Color> _sRgbLookUp = new();
    private static readonly RenderTargetFormatParameters RenderTargetFormatParameters = new(RenderTargetColorFormat.Rgba8Srgb);

    private static readonly List<Vector2> TileLinesToDraw = [];
    private static readonly List<Vector2> TileRectsToDraw = [];

    private const float UpdateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
        _navMap.WallColor = new Color(102, 164, 217);
        _navMap.TileColor = new Color(30, 57, 67);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.StencilTexture?.Texture.Size != args.Viewport.Size)
        {
            res.StaticTexture?.Dispose();
            res.StencilTexture?.Dispose();
            res.StencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, RenderTargetFormatParameters, name: "station-ai-stencil");
            res.StaticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                RenderTargetFormatParameters,
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entManager.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float) _timing.FrameTime.TotalSeconds;

        if (grid != null && broadphase != null)
        {
            var lookups = _entManager.System<EntityLookupSystem>();
            var xforms = _entManager.System<SharedTransformSystem>();

            _navMap.AiFrameUpdate((float) _timing.FrameTime.TotalSeconds, gridUid);
            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + UpdateRate);
                _visibleTiles.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(res.StencilTexture!,
                () =>
            {
                worldHandle.SetTransform(matty);

                foreach (var tile in _visibleTiles)
                {
                    var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                    worldHandle.DrawRect(aabb, Color.White);
                }
            },
            Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(res.StaticTexture!,
            () =>
            {
                worldHandle.SetTransform(matty);

                DrawNavMap(worldHandle);
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(res.StencilTexture!,
                () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(res.StaticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            },
            Color.Black);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(res.StencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index(StencilDrawShader).Instance());
        worldHandle.DrawTextureRect(res.StaticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? StaticTexture;
        public IRenderTexture? StencilTexture;

        public void Dispose()
        {
            StaticTexture?.Dispose();
            StencilTexture?.Dispose();
        }
    }

    private void DrawNavMap(DrawingHandleWorld handle)
    {
        if (!_sRgbLookUp.TryGetValue(_navMap.WallColor, out var wallsRgb))
        {
            wallsRgb = Color.ToSrgb(_navMap.WallColor);
            _sRgbLookUp[_navMap.WallColor] = wallsRgb;
        }

        // Draw floor tiles
        if (_navMap.TilePolygons.Count != 0)
        {
            foreach (var (polygonVerts, polygonColor) in _navMap.TilePolygons)
            {
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, polygonVerts.AsSpan()[..], polygonColor);
            }
        }

        // Draw map lines
        if (_navMap.TileLines.Count != 0)
        {
            TileLinesToDraw.Clear();
            TileLinesToDraw.EnsureCapacity(_navMap.TileLines.Count * 2);

            foreach (var (o, t) in _navMap.TileLines)
            {
                var origin = o with { Y = -o.Y };
                var terminus = t with { Y = -t.Y };

                TileLinesToDraw.Add(origin);
                TileLinesToDraw.Add(terminus);
            }

            if (TileLinesToDraw.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, TileLinesToDraw, wallsRgb);
        }

        // Draw map rects
        if (_navMap.TileRects.Count != 0)
        {
            TileRectsToDraw.Clear();
            TileRectsToDraw.EnsureCapacity(_navMap.TileRects.Count * 8);

            foreach (var (lt, rb) in _navMap.TileRects)
            {
                var leftTop = lt with { Y = -lt.Y };
                var rightBottom = rb with { Y = -rb.Y };

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                TileRectsToDraw.Add(leftTop);
                TileRectsToDraw.Add(rightTop);
                TileRectsToDraw.Add(rightTop);
                TileRectsToDraw.Add(rightBottom);
                TileRectsToDraw.Add(rightBottom);
                TileRectsToDraw.Add(leftBottom);
                TileRectsToDraw.Add(leftBottom);
                TileRectsToDraw.Add(leftTop);
            }

            if (TileRectsToDraw.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, TileRectsToDraw, wallsRgb);
        }
    }
}
