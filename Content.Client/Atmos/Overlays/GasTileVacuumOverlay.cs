using Content.Client.Graphics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Atmos.Overlays;

/// <summary>
///     Overlay responsible for rendering vacuum overlay.
/// </summary>
public sealed partial class GasTileVacuumOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = true;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> VacuumOverlayShader = "VacuumDesaturation";
    private static readonly Color EmptyColor = new Color(0, 0, 0, 0);
    private static readonly Color MarkerColor = new Color(255f, 0, 0);
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _configManager = default!;

    private readonly SharedMapSystem _maps;
    private readonly SharedTransformSystem _xformSys;
    private readonly ShaderInstance _unshader;
    private readonly ShaderInstance _shader;

    private List<Entity<MapGridComponent>> _intersectingGrids = new();
    private readonly OverlayResourceCache<CachedResources> _resources = new();

    // Overlay settings
    private float _intensity = 0f; // overlay intensity. 0.0f = turned off, 1.0f = full grayscale

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public GasTileVacuumOverlay()
    {
        IoCManager.InjectDependencies(this);
        _maps = _entManager.System<SharedMapSystem>();
        _xformSys = _entManager.System<SharedTransformSystem>();

        _unshader = _proto.Index(UnshadedShader).Instance();
        _shader = _proto.Index(VacuumOverlayShader).InstanceUnique();
        _configManager.OnValueChanged(CCVars.VacuumOverlayIntensity, SetVacuumOverlayIntensity, invokeImmediately: true);
    }

    private void SetVacuumOverlayIntensity(float intensity)
    {
        _intensity = MathHelper.Clamp(intensity, 0f, 1f);
        _shader.SetParameter("intensity", _intensity);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace || _intensity < 0.1f)
            return false;

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        var target = args.Viewport.RenderTarget;

        // Probably the resolution of the game window changed, remake the textures.
        if (res.VacuumTarget?.Texture.Size != target.Size)
        {
            res.VacuumTarget?.Dispose();
            res.VacuumTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: $"{nameof(GasTileVacuumOverlay)}-blur");
        }

        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        args.WorldHandle.UseShader(_unshader);

        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        // If there is no Vacuum after checking all visible tiles, we can bail early
        var anyVacuum = false;

        // We're rendering in the context of the vacuum target texture, which will encode data as to where vacuum effect will be
        args.WorldHandle.RenderInRenderTarget(res.VacuumTarget,
            () =>
            {
                _intersectingGrids.Clear();
                _maps.FindGridsIntersecting(mapId, worldAABB, ref _intersectingGrids);
                foreach (var grid in _intersectingGrids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt))
                        continue;

                    // Draw commands (like DrawRect) will be using grid coordinates from here
                    worldHandle.SetTransform(gridEntToViewportLocal);

                    // We only care about tiles that fit in these bounds
                    var worldToGridLocal = _xformSys.GetInvWorldMatrix(grid.Owner);
                    var floatBounds = worldToGridLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);

                    var localBounds = new Box2i(
                        (int)MathF.Floor(floatBounds.Left),
                        (int)MathF.Floor(floatBounds.Bottom),
                        (int)MathF.Ceiling(floatBounds.Right),
                        (int)MathF.Ceiling(floatBounds.Top));

                    // for each tile and its gas --->
                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);

                        while (enumerator.MoveNext(out var tileGas))
                        {
                            // Check and make sure the tile is within the viewport/screen
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;

                            if (tileGas.ByteGasTemperature.Value == ThermalByte.StateVacuum)
                            {
                                // Encode the strength in the red channel
                                // alpha set to 1 as tile is active
                                worldHandle.DrawRect(
                                    Box2.CenteredAround(tilePosition + grid.Comp.TileSizeHalfVector,
                                        grid.Comp.TileSizeVector), MarkerColor);
                                anyVacuum = true;
                            }
                        }
                    }
                }
            },
            // This clears the buffer to all zero first...
            EmptyColor);

        // no distortion, no need to render
        if (!anyVacuum)
        {
            args.WorldHandle.UseShader(null);
            args.WorldHandle.SetTransform(Matrix3x2.Identity);
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (ScreenTexture is null || res.VacuumTarget is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawTextureRect(res.VacuumTarget.Texture, args.WorldBounds);

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        _configManager.UnsubValueChanged(CCVars.VacuumOverlayIntensity, SetVacuumOverlayIntensity);
        base.DisposeBehavior();
    }

    internal sealed class CachedResources : IDisposable
    {
        public IRenderTexture? VacuumTarget;
        public void Dispose()
        {
            VacuumTarget?.Dispose();
        }
    }
}
