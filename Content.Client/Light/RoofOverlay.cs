using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    private readonly IEntityManager _entManager;

    private readonly HashSet<Entity<OccluderComponent>> _occluders = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public RoofOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);
        ZIndex = -1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;
        var mapSystem = _entManager.System<SharedMapSystem>();
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var mapId = args.MapId;

        var lookup = _entManager.System<EntityLookupSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();

        var query = _entManager.AllEntityQueryEnumerator<RoofComponent, MapGridComponent, TransformComponent>();
        var target = IoCManager.Resolve<IClyde>()
            .CreateLightRenderTarget(viewport.LightRenderTarget.Size, name: "roof-target");

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                var invMatrix = target.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);

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
                        var tileDef = (ContentTileDefinition) _tileDefMan[tileRef.Tile.TypeId];

                        if (!tileDef.Roof)
                        {
                            // Check if the tile is occluded in which case hide it anyway.
                            // This is to avoid lit walls bleeding over to unlit tiles.
                            _occluders.Clear();
                            lookup.GetLocalEntitiesIntersecting(uid, tileRef.GridIndices, _occluders);
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

                        var local = lookup.GetLocalBounds(tileRef, grid.TileSize);
                        worldHandle.DrawRect(local, comp.Color);
                    }
                }
            }, null);

        //IoCManager.Resolve<IClyde>().BlurLights(viewport, target, viewport.Eye, 14f * 4f);

        worldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
        () =>
        {
            var invMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale / 2f);
            worldHandle.SetTransform(invMatrix);
            worldHandle.DrawTextureRect(target.Texture, bounds);
        }, null);

        // Around half-a-tile in length because too lazy to do shadows properly and this avoids it going through walls.
        // IoCManager.Resolve<IClyde>().BlurLights(viewport, viewport.LightRenderTarget, viewport.Eye, 14f * 4f);
    }
}
