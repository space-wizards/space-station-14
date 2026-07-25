using System.Numerics;
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

    private readonly OverlayResourceCache<CachedResources> _resources = new();

    // DS14-start
    private readonly EntityUid _owner;
    private EntityUid _lastGrid = EntityUid.Invalid;
    // DS14-end

    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay(EntityUid owner) // DS14
    {
        IoCManager.InjectDependencies(this);
        _owner = owner; // DS14
    }

    // DS14-start
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity != _owner ||
            !_entManager.HasComponent<StationAiOverlayComponent>(_owner) ||
            !_entManager.TryGetComponent<EyeComponent>(_owner, out var eye) ||
            args.Viewport.Eye != eye.Eye)
        {
            _visibleTiles.Clear();
            _lastGrid = EntityUid.Invalid;
            _accumulator = 0f;
            return false;
        }

        return base.BeforeDraw(in args);
    }
    // DS14-end

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
        // DS14-start
        if (_lastGrid != gridUid)
        {
            _lastGrid = gridUid;
            _visibleTiles.Clear();
            _accumulator = 0f;
        }
        // DS14-end
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
                var shader = _proto.Index(CameraStaticShader).Instance();
                worldHandle.UseShader(shader);
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
}
