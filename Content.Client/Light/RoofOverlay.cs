using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _xformSystem;

    private readonly HashSet<Entity<OccluderComponent>> _occluders = new();
    private List<Entity<MapGridComponent>> _grids = new();

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

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;

        var worldHandle = args.WorldHandle;
        var lightoverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = lightoverlay.EnlargedBounds;
        var target = lightoverlay.EnlargedLightTarget;

        _grids.Clear();
        _mapManager.FindGridsIntersecting(args.MapId, bounds, ref _grids);

        for (var i = _grids.Count - 1; i >= 0; i--)
        {
            var grid = _grids[i];

            if (_entManager.HasComponent<RoofComponent>(grid.Owner))
                continue;

            _grids.RemoveAt(i);
        }

        if (_grids.Count == 0)
            return;

        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                foreach (var grid in _grids)
                {
                    if (!_entManager.TryGetComponent(grid.Owner, out RoofComponent? roof))
                        continue;

                    var invMatrix = target.GetWorldToLocalMatrix(eye, scale);

                    var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    var tileEnumerator = _mapSystem.GetTilesEnumerator(grid.Owner, grid, bounds);

                    // Due to stencilling we essentially draw on unrooved tiles
                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        if ((tileRef.Tile.Flags & (byte) TileFlag.Roof) == 0x0)
                        {
                            // Check if the tile is occluded in which case hide it anyway.
                            // This is to avoid lit walls bleeding over to unlit tiles.
                            _occluders.Clear();
                            _lookup.GetLocalEntitiesIntersecting(grid.Owner, tileRef.GridIndices, _occluders);
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

                        var local = _lookup.GetLocalBounds(tileRef, grid.Comp.TileSize);
                        worldHandle.DrawRect(local, roof.Color);
                    }
                }
            }, null);

        worldHandle.SetTransform(Matrix3x2.Identity);
    }
}
