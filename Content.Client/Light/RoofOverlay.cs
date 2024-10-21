using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly IEntityManager _entManager;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _xformSystem;

    private readonly HashSet<Entity<OccluderComponent>> _occluders = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    private IRenderTexture? _target;

    public RoofOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();

        ZIndex = -1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var mapEnt = _mapSystem.GetMap(args.MapId);

        if (!_entManager.TryGetComponent(mapEnt, out RoofComponent? roofComp) ||
            !_entManager.TryGetComponent(mapEnt, out MapGridComponent? grid))
        {
            return;
        }

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;

        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;

        if (_target?.Size != viewport.LightRenderTarget.Size)
        {
            _target = _clyde
                .CreateRenderTarget(viewport.LightRenderTarget.Size,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "roof-target");
        }

        worldHandle.RenderInRenderTarget(_target,
            () =>
            {
                var invMatrix = _target.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);

                var gridMatrix = _xformSystem.GetWorldMatrix(mapEnt);
                var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                worldHandle.SetTransform(matty);

                var tileEnumerator = _mapSystem.GetTilesEnumerator(mapEnt, grid, bounds);

                // Due to stencilling we essentially draw on unrooved tiles
                while (tileEnumerator.MoveNext(out var tileRef))
                {
                    if ((tileRef.Tile.ContentFlag & (ushort) TileFlag.Roof) == 0x0)
                    {
                        // Check if the tile is occluded in which case hide it anyway.
                        // This is to avoid lit walls bleeding over to unlit tiles.
                        _occluders.Clear();
                        _lookup.GetLocalEntitiesIntersecting(mapEnt, tileRef.GridIndices, _occluders);
                        var found = false;

                        foreach (var occluder in _occluders)
                        {
                            if (!occluder.Comp.Enabled)
                                continue;

                            found = true;
                            break;
                        }

                        if (!found)
                            continue;
                    }

                    var local = _lookup.GetLocalBounds(tileRef, grid.TileSize);
                    worldHandle.DrawRect(local, roofComp.Color);
                }

            }, Color.Transparent);

        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var invMatrix =
                    viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
                worldHandle.SetTransform(invMatrix);

                var maskShader = _protoManager.Index<ShaderPrototype>("Mix").Instance();
                worldHandle.UseShader(maskShader);

                worldHandle.DrawTextureRect(_target.Texture, bounds);
            }, null);
    }
}
