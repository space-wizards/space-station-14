using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using static Content.Shared.Mapping.RoofDebugOverlaySystem;

namespace Content.Client.Mapping;


public sealed partial class RoofDebugOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transform;
    private readonly ClientRoofDebugOverlaySystem _system;
    private readonly SharedMapSystem _map;
    private readonly Font _font;
    private List<(Entity<MapGridComponent>, RoofDebugOverlayMessage)> _grids = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    internal RoofDebugOverlay(ClientRoofDebugOverlaySystem system)
    {
        IoCManager.InjectDependencies(this);

        _system = system;
        _transform = _entManager.System<SharedTransformSystem>();
        _map = _entManager.System<SharedMapSystem>();
        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        GetGrids(args.MapId, args.WorldBounds);

        foreach (var (grid, msg) in _grids)
        {
            handle.SetTransform(_transform.GetWorldMatrix(grid));
            DrawData(msg, handle);
        }
    }

    private void DrawData(RoofDebugOverlayMessage msg,
        DrawingHandleWorld handle)
    {
        foreach (var data in msg.OverlayData)
        {
            if (data != null)
                DrawGridTile(data.Value, handle);
        }
    }

    private void DrawGridTile(RoofDebugOverlayData data,
        DrawingHandleWorld handle)
    {
        Draw(data, handle);
    }

    private void Draw(RoofDebugOverlayData data, DrawingHandleWorld handle)
    {
        var tile = data.Indices;

        if (data.HasEntity)
            handle.DrawRect(Box2.FromDimensions(new Vector2(tile.X + 0.1f, tile.Y + 0.1f), new Vector2(0.8f, 0.8f)), Color.Cyan.WithAlpha(0.10f));

        Color res;
        if (data.HasRoof)
            res = Color.Yellow;
        else if (data.Innate)
            res = Color.Red;
        else
            return;

        res = res.WithAlpha(0.10f);

        handle.DrawRect(Box2.FromDimensions(new Vector2(tile.X, tile.Y), new Vector2(1, 1)), res);
    }

    private void GetGrids(MapId mapId, Box2Rotated box)
    {
        // i genuinely have no idea what the fuck i'm doing
        _grids.Clear();
        _map.FindGridsIntersecting(
            mapId,
            box,
            ref _grids,
            (EntityUid uid,
                MapGridComponent grid,
            ref List<(Entity<MapGridComponent>, RoofDebugOverlayMessage)> state) =>
        {
            if (_system.TileData.TryGetValue(uid, out var data))
                state.Add(((uid, grid), data));
            return true;
        });
    }
}
