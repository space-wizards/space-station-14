// Modified by Ronstation contributor(s), therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
using System.Numerics;
using System.Linq; // Ronstation - modification.
using Content.Client.Pinpointer.UI; // Ronstation - modification.
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Collections; // Ronstation - modification.
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CameraStaticShader = "CameraStatic";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawShader = "StencilDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();
    private readonly NavMapControl _navMap = new(); // Ronstation - modification.

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;
    private Dictionary<Color, Color> _sRGBLookUp = new(); // Ronstation - modification.

    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
        _navMap.WallColor = new(102, 102, 102); // Ronstation - modification.
        _navMap.TileColor = new(30, 30, 30); // Ronstation - modification.
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
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

            _navMap.AiFrameUpdate((float) _timing.FrameTime.TotalSeconds, gridUid); // Ronstation - modification.

            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + _updateRate);
                _visibleTiles.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
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
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(matty); // Ronstation - modification.
                DrawNavMap(worldHandle, grid); // Ronstation - modification.
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index(StencilDrawShader).Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }

    // Ronstation - start of modifications.
    protected void DrawNavMap(DrawingHandleWorld handle, MapGridComponent grid)
    {   
        if (!_sRGBLookUp.TryGetValue(_navMap.WallColor, out var wallsRGB))
        {
            wallsRGB = Color.ToSrgb(_navMap.WallColor);
            _sRGBLookUp[_navMap.WallColor] = wallsRGB;
        }

        // Draw floor tiles
        if (_navMap.TilePolygons.Any())
        {
            foreach (var (polygonVerts, polygonColor) in _navMap.TilePolygons)
            {
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, polygonVerts[..polygonVerts.Length], polygonColor);
            }
        }

        // Draw map lines
        if (_navMap.TileLines.Any())
        {
            var lines = new ValueList<Vector2>(_navMap.TileLines.Count * 2);

            foreach (var (o, t) in _navMap.TileLines)
            {
                var origin = new Vector2(o.X, -o.Y);
                var terminus = new Vector2(t.X, -t.Y);

                lines.Add(origin);
                lines.Add(terminus);
            }

            if (lines.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, lines.Span, wallsRGB);
        }

        // Draw map rects
        if (_navMap.TileRects.Any())
        {
            var rects = new ValueList<Vector2>(_navMap.TileRects.Count * 8);

            foreach (var (lt, rb) in _navMap.TileRects)
            {
                var leftTop = new Vector2(lt.X, -lt.Y);
                var rightBottom = new Vector2(rb.X, -rb.Y);

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                rects.Add(leftTop);
                rects.Add(rightTop);
                rects.Add(rightTop);
                rects.Add(rightBottom);
                rects.Add(rightBottom);
                rects.Add(leftBottom);
                rects.Add(leftBottom);
                rects.Add(leftTop);
            }

            if (rects.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, rects.Span, wallsRGB);
        }
    }
    // Ronstation - end of modifications.
}
