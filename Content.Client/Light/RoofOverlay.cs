using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed class RoofOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedRoofSystem _roof = default!;
    private readonly SharedTransformSystem _xformSystem;

    private List<Entity<MapGridComponent>> _grids = new();

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public const int ContentZIndex = BeforeLightTargetOverlay.ContentZIndex + 1;

    public RoofOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _roof = _entManager.System<SharedRoofSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();

        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || !_entManager.HasComponent<MapLightComponent>(args.MapUid))
            return;

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;

        var worldHandle = args.WorldHandle;
        var lightoverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = lightoverlay.EnlargedBounds;
        var target = lightoverlay.EnlargedLightTarget;

        _grids.Clear();
        _mapManager.FindGridsIntersecting(args.MapId, bounds, ref _grids, approx: true, includeMap: true);
        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        worldHandle.RenderInRenderTarget(target,
            () =>
            {
                for (var i = 0; i < _grids.Count; i++)
                {
                    var grid = _grids[i];

                    if (!_entManager.TryGetComponent(grid.Owner, out ImplicitRoofComponent? roof))
                        continue;

                    var invMatrix = target.GetWorldToLocalMatrix(eye, scale);

                    var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
                    var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

                    worldHandle.SetTransform(matty);

                    var tileEnumerator = _mapSystem.GetTilesEnumerator(grid.Owner, grid, bounds);
                    var color = roof.Color;

                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        var local = _lookup.GetLocalBounds(tileRef, grid.Comp.TileSize);
                        worldHandle.DrawRect(local, color);
                    }

                    // Don't need it for the next stage.
                    _grids.RemoveAt(i);
                    i--;
                }
            }, null);

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
                    var roofEnt = (grid.Owner, grid.Comp, roof);

                    // Due to stencilling we essentially draw on unrooved tiles
                    while (tileEnumerator.MoveNext(out var tileRef))
                    {
                        var color = _roof.GetColor(roofEnt, tileRef.GridIndices);

                        if (color == null)
                        {
                            continue;
                        }

                        var local = _lookup.GetLocalBounds(tileRef, grid.Comp.TileSize);
                        worldHandle.DrawRect(local, color.Value);
                    }
                }
            }, null);

        worldHandle.SetTransform(Matrix3x2.Identity);
    }
}
