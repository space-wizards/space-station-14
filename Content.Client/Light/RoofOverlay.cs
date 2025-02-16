using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _xformSystem;

    private readonly HashSet<Entity<OccluderComponent>> _occluders = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public const int ContentZIndex = BeforeLightTargetOverlay.ContentZIndex + 1;

    public RoofOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();

        ZIndex = ContentZIndex;
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
        var lightoverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = lightoverlay.EnlargedBounds;
        var target = lightoverlay.EnlargedLightTarget;

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                var invMatrix = target.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);

                var gridMatrix = _xformSystem.GetWorldMatrix(mapEnt);
                var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                worldHandle.SetTransform(matty);

                var tileEnumerator = _mapSystem.GetTilesEnumerator(mapEnt, grid, bounds);

                // Due to stencilling we essentially draw on unrooved tiles
                while (tileEnumerator.MoveNext(out var tileRef))
                {
                    if ((tileRef.Tile.Flags & (byte) TileFlag.Roof) == 0x0)
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

            }, null);
    }
}
