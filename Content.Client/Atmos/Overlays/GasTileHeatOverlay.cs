using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
namespace Content.Client.Atmos.Overlays;

public sealed class GasTileHeatOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = false;

    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> HeatOverlayShader = "Heat";

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
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

    private Color GetGasColor(float tempK)
    {
        float tempC = tempK - 273.15f;
        // Optimization: return Transparent for safe temps immediately
        if (tempC >= 0f && tempC < 50f) return Color.Transparent;

        if (tempC < -150f) return Color.FromHex("#330066");
        if (tempC < -50f) return Color.InterpolateBetween(Color.FromHex("#330066"), Color.Blue, (tempC + 150f) / 100f);
        if (tempC < 0f) return Color.InterpolateBetween(Color.Blue, Color.Transparent, (tempC + 50f) / 50f);
        if (tempC < 100f) return Color.InterpolateBetween(Color.Transparent, Color.Yellow, (tempC - 50f) / 50f);
        if (tempC < 300f) return Color.InterpolateBetween(Color.Yellow, Color.Red, (tempC - 100f) / 200f);
        return Color.DarkRed;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace) return false;

        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();
        if (_gasTileOverlay == null) return false;

        var target = args.Viewport.RenderTarget;

        // --- 1. PREPARE RESOURCES ---
        if (_heatTarget?.Texture.Size != target.Size)
        {
            _heatTarget?.Dispose();
            _heatTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileHeatOverlay));
        }

        var drawHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;
        var worldAABB = args.WorldAABB;
        var mapId = args.MapId;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        drawHandle.UseShader(_proto.Index(UnshadedShader).Instance());

        bool anyGasDrawn = false;

        // --- 2. RENDER INTO TARGET ---
        drawHandle.RenderInRenderTarget(_heatTarget,
            () =>
            {
                // Explicitly clear the target with a transparent color
                // Note: We use the RenderInRenderTarget's clear parameter below, 
                // but this lambda must also NOT draw garbage.

                List<Entity<MapGridComponent>> grids = new();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);

                foreach (var grid in grids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp)) continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt)) continue;

                    var uvToUi = Matrix3Helpers.CreateScale(_heatTarget.Size.X, -_heatTarget.Size.Y);
                    var uvToGridEnt = uvToUi * viewportLocalToGridEnt;

                    _shader.SetParameter("grid_ent_from_viewport_local", uvToGridEnt);
                    drawHandle.SetTransform(gridEntToViewportLocal);

                    var floatBounds = worldToViewportLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);
                    var localBounds = new Box2i(
                        (int)MathF.Floor(floatBounds.Left),
                        (int)MathF.Floor(floatBounds.Bottom),
                        (int)MathF.Ceiling(floatBounds.Right),
                        (int)MathF.Ceiling(floatBounds.Top));

                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);
                        while (enumerator.MoveNext(out var tileGas))
                        {
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition)) continue;

                            Color gasColor = GetGasColor(tileGas.Temperature);

                            // Skip Transparent colors (Safe Zones)
                            // This prevents drawing "invisible" squares that might mess up blending
                            if (gasColor.A <= 0f) continue;

                            anyGasDrawn = true;

                            drawHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + new Vector2(0.5f, 0.5f), grid.Comp.TileSizeVector),
                                gasColor
                            );
                        }
                    }
                }
            },
            new Color(0, 0, 0, 0)); // Clear color: Transparent Black

        drawHandle.UseShader(null);
        drawHandle.SetTransform(Matrix3x2.Identity);

        // --- 3. RESIDUE FIX ---
        // If no gas was actually drawn in the loop, DESTROY the texture.
        // This guarantees the screen is wiped clean.
        if (!anyGasDrawn)
        {
            _heatTarget?.Dispose();
            _heatTarget = null;
            return false; // Stop here, don't run Draw()
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // If target is null (because we disposed it in BeforeDraw), do nothing.
        if (_heatTarget is null)
            return;

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawTextureRect(_heatTarget.Texture, args.WorldBounds);
        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _heatTarget?.Dispose(); // Ensure we clean up
        _heatTarget = null;
        base.DisposeBehavior();
    }
}
