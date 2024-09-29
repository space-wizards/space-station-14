using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class PlanetLightOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var entManager = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var viewport = args.Viewport;
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var lookup = entManager.System<EntityLookupSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var query = entManager.AllEntityQueryEnumerator<PlanetLightComponent, MapGridComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var grid, out var xform))
        {
            if (args.MapId != xform.MapID)
                continue;

            var gridMatrix = xformSystem.GetWorldMatrix(uid);
            var matrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(args.Viewport.Eye, args.Viewport.RenderScale / 2f);
            var matty = Matrix3x2.Multiply(matrix, gridMatrix);

            args.WorldHandle.RenderInRenderTarget(args.Viewport.LightRenderTarget,
                () =>
                {
                    worldHandle.SetTransform(matty);
                    var tileEnumerator = mapSystem.GetTilesEnumerator(uid, grid, bounds);

                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        if (tileRef.Tile.TypeId == 126)
                            continue;

                        var local = lookup.GetLocalBounds(tileRef, grid.TileSize);
                        worldHandle.DrawRect(local, comp.Color);
                    }
                }, null);
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
