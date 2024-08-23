using System.Numerics;
using Content.Client.Pinpointer;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;

    protected NavMapData _data = new();

    // Only update the frame-data at a lower rate in case someone has a billion hz monitor.
    private float _accumulator;

    private const float UpdateRate = 1f / 30f;

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
        var lookups = _entManager.System<EntityLookupSystem>();
        var xforms = _entManager.System<SharedTransformSystem>();

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        if (grid != null)
        {
            _accumulator -= (float) IoCManager.Resolve<IGameTiming>().FrameTime.TotalSeconds;

            if (_accumulator <= 0f)
            {
                _accumulator += UpdateRate;
                // TODO: I hate this shit
                _data.UpdateNavMap((gridUid, grid));

                // TODO: Pass in attached entity's grid.
                // TODO: Credit OD on the moved to code
                // TODO: Call the moved-to code here.

                _visibleTiles.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, grid), worldBounds, _visibleTiles);
            }

            var (_, _, gridMatrix, gridInvMatrix) = xforms.GetWorldPositionRotationMatrixWithInv(gridUid);
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

            // Create background texture.
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(invMatrix);
                worldHandle.DrawRect(worldBounds, Color.Black);
                worldHandle.SetTransform(matty);
                var localAABB = gridInvMatrix.TransformBox(worldBounds);

                _data.Draw(worldHandle, vec => new Vector2(vec.X, -vec.Y), localAABB);
            }, Color.Transparent);
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
