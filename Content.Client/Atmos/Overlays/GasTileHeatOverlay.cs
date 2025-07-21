using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Atmos.Overlays;

public sealed class GasTileHeatOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = true;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> HeatOverlayShader = "Heat";

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    private readonly SharedTransformSystem _xformSys;

    private IRenderTexture? _heatTarget;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    private readonly ShaderInstance _shader;


    public GasTileHeatOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();

        _shader = _proto.Index(HeatOverlayShader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;
        if (ScreenTexture is null)
            return;

        var target = args.Viewport.RenderTarget;

        if (_heatTarget?.Texture.Size != target.Size)
        {
            _heatTarget?.Dispose();
            _heatTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileHeatOverlay));
        }

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        args.WorldHandle.UseShader(_proto.Index(UnshadedShader).Instance());

        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        args.WorldHandle.RenderInRenderTarget(_heatTarget,
            () =>
            {
                List<Entity<MapGridComponent>> grids = new();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);
                foreach (var grid in grids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        return;

                    if (!xformQuery.TryGetComponent(grid.Owner, out var gridXform))
                        return;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt))
                        return;

                    var uvToUi = Matrix3Helpers.CreateScale(_heatTarget.Size.X, -_heatTarget.Size.Y);
                    var uvToGridEnt = uvToUi * viewportLocalToGridEnt;

                    _shader.SetParameter("grid_ent_from_viewport_local", uvToGridEnt);

                    worldHandle.SetTransform(gridEntToViewportLocal);

                    var floatBounds = worldToViewportLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);
                    var localBounds = new Box2i(
                        (int) MathF.Floor(floatBounds.Left),
                        (int) MathF.Floor(floatBounds.Bottom),
                        (int) MathF.Ceiling(floatBounds.Right),
                        (int) MathF.Ceiling(floatBounds.Top));

                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);

                        while (enumerator.MoveNext(out var tileGas))
                        {
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;
                            var strength = MathHelper.Clamp01((tileGas.Temperature - 320.0f)/1000.0f);
                            worldHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + new Vector2(0.5f, 0.5f), grid.Comp.TileSizeVector),
                                Color.White.WithAlpha(strength));
                        }
                    }
                }
            },
            Color.Transparent);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawTextureRect(_heatTarget.Texture, args.WorldBounds);

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
