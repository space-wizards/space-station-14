using System.Linq;
using System.Numerics;
using Content.Client.Radiation.Systems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.Radiation.Overlays;

public sealed class RadiationDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly RadiationSystem _radiation;

    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public RadiationDebugOverlay()
    {
        IoCManager.InjectDependencies(this);
        _radiation = _entityManager.System<RadiationSystem>();

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.ScreenSpace:
                DrawScreenRays(args);
                DrawScreenResistance(args);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
        }
    }

    private void DrawScreenRays(OverlayDrawArgs args)
    {
        var rays = _radiation.Rays;
        if (rays == null || args.ViewportControl == null)
            return;

        var handle = args.ScreenHandle;
        foreach (var ray in rays)
        {
            if (ray.MapId != args.MapId)
                continue;

            if (ray.ReachedDestination)
            {
                var screenCenter = args.ViewportControl.WorldToScreen(ray.Destination);
                handle.DrawString(_font, screenCenter, ray.Rads.ToString("F2"), 2f, Color.White);
            }

            foreach (var (netGrid, blockers) in ray.Blockers)
            {
                var gridUid = _entityManager.GetEntity(netGrid);

                if (!_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                    continue;

                foreach (var (tile, rads) in blockers)
                {
                    var worldPos = grid.GridTileToWorldPos(tile);
                    var screenCenter = args.ViewportControl.WorldToScreen(worldPos);
                    handle.DrawString(_font, screenCenter, rads.ToString("F2"), 1.5f, Color.White);
                }
            }
        }
    }

    private void DrawScreenResistance(OverlayDrawArgs args)
    {
        var resistance = _radiation.ResistanceGrids;
        if (resistance == null || args.ViewportControl == null)
            return;

        var handle = args.ScreenHandle;
        var query = _entityManager.GetEntityQuery<TransformComponent>();
        foreach (var (netGrid, resMap) in resistance)
        {
            var gridUid = _entityManager.GetEntity(netGrid);

            if (!_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                continue;
            if (query.TryGetComponent(gridUid, out var trs) && trs.MapID != args.MapId)
                continue;

            var offset = new Vector2(grid.TileSize, -grid.TileSize) * 0.25f;
            foreach (var (tile, value) in resMap)
            {
                var localPos = grid.GridTileToLocal(tile).Position + offset;
                var worldPos = grid.LocalToWorld(localPos);
                var screenCenter = args.ViewportControl.WorldToScreen(worldPos);
                handle.DrawString(_font, screenCenter, value.ToString("F2"), color: Color.White);
            }
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        var rays = _radiation.Rays;
        if (rays == null)
            return;

        var handle = args.WorldHandle;
        // draw lines for raycasts
        foreach (var ray in rays)
        {
            if (ray.MapId != args.MapId)
                continue;

            if (ray.ReachedDestination)
            {
                handle.DrawLine(ray.Source, ray.Destination, Color.Red);
                continue;
            }

            foreach (var (netGrid, blockers) in ray.Blockers)
            {
                var gridUid = _entityManager.GetEntity(netGrid);

                if (!_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                    continue;
                var (destTile, _) = blockers.Last();
                var destWorld = grid.GridTileToWorldPos(destTile);
                handle.DrawLine(ray.Source, destWorld, Color.Red);
            }
        }
    }
}
