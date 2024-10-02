using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public RoofOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;
        var entManager = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var mapId = args.MapId;

        var lookup = entManager.System<EntityLookupSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var query = entManager.AllEntityQueryEnumerator<RoofComponent, MapGridComponent, TransformComponent>();

        worldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var invMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);

                while (query.MoveNext(out var uid, out var comp, out var grid, out var xform))
                {
                    if (mapId != xform.MapID)
                        continue;

                    var gridMatrix = xformSystem.GetWorldMatrix(uid);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    var tileEnumerator = mapSystem.GetTilesEnumerator(uid, grid, bounds);

                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        if (tileRef.Tile.TypeId != 126)
                            continue;

                        var local = lookup.GetLocalBounds(tileRef, grid.TileSize);
                        worldHandle.DrawRect(local, comp.Color);
                    }
                }
            }, null);
    }
}
