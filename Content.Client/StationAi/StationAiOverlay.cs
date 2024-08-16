using System.Numerics;
using Content.Client.SurveillanceCamera;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private IRenderTexture? _staticTexture;
    public IRenderTexture? _stencilTexture;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
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
        var maps = _entManager.System<SharedMapSystem>();
        var lookups = _entManager.System<EntityLookupSystem>();
        var xforms = _entManager.System<SharedTransformSystem>();

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        if (grid != null)
        {
            // TODO: Pass in attached entity's grid.
            // TODO: Credit OD on the moved to code
            // TODO: Call the moved-to code here.

            _visibleTiles.Clear();
            _entManager.System<StationAiVisionSystem>().GetView((gridUid, grid), worldBounds, _visibleTiles);

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
                {
                    if (!gridUid.IsValid())
                        return;

                    worldHandle.SetTransform(matty);

                    foreach (var tile in _visibleTiles)
                    {
                        var aabb = lookups.GetLocalBounds(tile, grid!.TileSize);
                        worldHandle.DrawRect(aabb, Color.White);
                    }
                },
                Color.Transparent);

            // Create static texture
            var curTime = IoCManager.Resolve<IGameTiming>().RealTime;

            var noiseTexture = _entManager.System<SpriteSystem>()
                .GetFrame(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/noise.rsi"), "noise"), curTime);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(_staticTexture!,
                () =>
                {
                    // TODO: Handle properly
                    if (!gridUid.IsValid())
                        return;

                    worldHandle.SetTransform(matty);

                    var tileEnumerator = maps.GetTilesEnumerator(gridUid, grid!, worldBounds, ignoreEmpty: false);

                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        if (_visibleTiles.Contains(tileRef.GridIndices))
                            continue;

                        var bounds = lookups.GetLocalBounds(tileRef, grid!.TileSize);
                        worldHandle.DrawTextureRect(noiseTexture, bounds, Color.White.WithAlpha(80));
                    }

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
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }
}
