using System.Numerics;
using Content.Client.Graphics;
using Content.Shared.CCVar;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StationAiStaticShader = "StationAiStatic";
    private static readonly ProtoId<ShaderPrototype> CameraStaticAccessibleShader = "CameraStaticAccessible";
    private static readonly ProtoId<ShaderPrototype> StationAiStaticStencilDrawShader = "StationAiStaticStencilDraw";
    private static readonly ProtoId<ShaderPrototype> StencilClearShader = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly ShaderInstance _cameraStaticShader;
    private readonly ShaderInstance _cameraStaticAccessibleShader;

    private ShaderInstance _activeShader;
    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cameraStaticShader = _proto.Index(StationAiStaticShader).InstanceUnique();
        _cameraStaticAccessibleShader = _proto.Index(CameraStaticAccessibleShader).InstanceUnique();
        _activeShader = _cameraStaticShader;

        _cfg.OnValueChanged(CCVars.DisableAiStatic, OnAiStaticChanged, invokeImmediately: true);

    }

    private void OnAiStaticChanged(bool toggle)
    {
        _activeShader = toggle ? _cameraStaticAccessibleShader : _cameraStaticShader;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.StencilTexture?.Texture.Size != args.Viewport.Size)
        {
            res.StaticTexture?.Dispose();
            res.StencilTexture?.Dispose();
            res.StencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            res.StaticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
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

            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + _updateRate);
                _visibleTiles.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(res.StencilTexture!, () =>
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
                worldHandle.SetTransform(invMatrix);
                worldBounds.GetCorners(out var bottomLeft, out var bottomRight, out _, out var topLeft);
                var worldXAxis = bottomRight - bottomLeft;
                var worldYAxis = topLeft - bottomLeft;
                _cameraStaticShader.SetParameter("worldOriginAndCellSize", new Vector4(
                    bottomLeft.X,
                    bottomLeft.Y,
                    1f / EyeManager.PixelsPerMeter,
                    0f));
                _cameraStaticShader.SetParameter("worldAxes", new Vector4(
                    worldXAxis.X,
                    worldXAxis.Y,
                    worldYAxis.X,
                    worldYAxis.Y));
                worldHandle.UseShader(_activeShader);
                worldHandle.DrawRect(worldBounds, Color.White);
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(res.StencilTexture!, () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(res.StaticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);
        }

        worldHandle.UseShader(_proto.Index(StencilClearShader).Instance());
        worldHandle.DrawRect(worldBounds, Color.White);

        // Use the AI vision tile mask as a stencil.
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(res.StencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index(StationAiStaticStencilDrawShader).Instance());
        worldHandle.DrawTextureRect(res.StaticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }

    protected override void DisposeBehavior()
    {
        _cfg.UnsubValueChanged(CCVars.DisableAiStatic, OnAiStaticChanged);
        _cameraStaticShader.Dispose();
        _cameraStaticAccessibleShader.Dispose();
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
}
