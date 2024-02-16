using System.Linq;
using System.Numerics;
using Content.Client.Atmos.EntitySystems;
using Content.Client.Resources;
using Content.Shared.Atmos;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using AtmosDebugOverlayData = Content.Shared.Atmos.EntitySystems.SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData;
using DebugMessage = Content.Shared.Atmos.EntitySystems.SharedAtmosDebugOverlaySystem.AtmosDebugOverlayMessage;

namespace Content.Client.Atmos.Overlays;


public sealed class AtmosDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transform;
    private readonly AtmosDebugOverlaySystem _system;
    private readonly SharedMapSystem _map;
    private readonly Font _font;
    private List<(Entity<MapGridComponent>, DebugMessage)> _grids = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    internal AtmosDebugOverlay(AtmosDebugOverlaySystem system)
    {
        IoCManager.InjectDependencies(this);

        _system = system;
        _transform = _entManager.System<SharedTransformSystem>();
        _map = _entManager.System<SharedMapSystem>();
        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.ScreenSpace)
        {
            DrawTooltip(args);
            return;
        }

        var handle = args.WorldHandle;
        GetGrids(args.MapId, args.WorldBounds);

        // IF YOU ARE ABOUT TO INTRODUCE CHUNKING OR SOME OTHER OPTIMIZATION INTO THIS CODE:
        //  -- THINK! --
        // 1. "Is this going to make a critical atmos debugging tool harder to debug itself?"
        // 2. "Is this going to do anything that could cause the atmos debugging tool to use resources, server-side or client-side, when nobody's using it?"
        // 3. "Is this going to make it harder for atmos programmers to add data that may not be chunk-friendly into the atmos debugger?"
        // Nanotrasen needs YOU! to avoid premature optimization in critical debugging tools - 20kdc

        foreach (var (grid, msg) in _grids)
        {
            handle.SetTransform(_transform.GetWorldMatrix(grid));
            DrawData(msg, handle);
        }

        handle.SetTransform(Matrix3.Identity);
    }

    private void DrawData(DebugMessage msg,
        DrawingHandleWorld handle)
    {
        foreach (var data in msg.OverlayData)
        {
            if (data != null)
                DrawGridTile(data.Value, handle);
        }
    }

    private void DrawGridTile(AtmosDebugOverlayData data,
        DrawingHandleWorld handle)
    {
        DrawFill(data, handle);
        DrawBlocked(data, handle);
    }

    private void DrawFill(AtmosDebugOverlayData data, DrawingHandleWorld handle)
    {
        var tile = data.Indices;
        var fill = GetFillData(data);
        var interp = (fill - _system.CfgBase) / _system.CfgScale;

        Color res;
        if (_system.CfgCBM)
        {
            // Greyscale interpolation
            res = Color.InterpolateBetween(Color.Black, Color.White, interp);
        }
        else
        {
            // Red-Green-Blue interpolation
            if (interp < 0.5f)
            {
                res = Color.InterpolateBetween(Color.Red, Color.LimeGreen, interp * 2);
            }
            else
            {
                res = Color.InterpolateBetween(Color.LimeGreen, Color.Blue, (interp - 0.5f) * 2);
            }
        }

        res = res.WithAlpha(0.75f);
        handle.DrawRect(Box2.FromDimensions(new Vector2(tile.X, tile.Y), new Vector2(1, 1)), res);
    }

    private float GetFillData(AtmosDebugOverlayData data)
    {
        if (data.Moles == null)
            return 0;

        switch (_system.CfgMode)
        {
            case AtmosDebugOverlayMode.TotalMoles:
                var total = 0f;
                foreach (var f in data.Moles)
                {
                    total += f;
                }

                return total;
            case AtmosDebugOverlayMode.GasMoles:
                return data.Moles[_system.CfgSpecificGas];
            default:
                return data.Temperature;
        }
    }

    private void DrawBlocked(AtmosDebugOverlayData data, DrawingHandleWorld handle)
    {
        var tile = data.Indices;
        var tileCentre = tile + 0.5f * Vector2.One;
        CheckAndShowBlockDir(data, handle, AtmosDirection.North, tileCentre);
        CheckAndShowBlockDir(data, handle, AtmosDirection.South, tileCentre);
        CheckAndShowBlockDir(data, handle, AtmosDirection.East, tileCentre);
        CheckAndShowBlockDir(data, handle, AtmosDirection.West, tileCentre);

        // -- Pressure Direction --
        if (data.PressureDirection != AtmosDirection.Invalid)
        {
            DrawPressureDirection(handle, data.PressureDirection, tileCentre, Color.Blue);
        }
        else if (data.LastPressureDirection != AtmosDirection.Invalid)
        {
            DrawPressureDirection(handle, data.LastPressureDirection, tileCentre, Color.LightGray);
        }

        // -- Excited Groups --
        if (data.InExcitedGroup is {} grp)
        {
            var basisA = tile;
            var basisB = tile + new Vector2(1.0f, 1.0f);
            var basisC = tile + new Vector2(0.0f, 1.0f);
            var basisD = tile + new Vector2(1.0f, 0.0f);
            var color = Color.White // Use first three nibbles for an unique color... Good enough?
                .WithRed(grp & 0x000F)
                .WithGreen((grp & 0x00F0) >> 4)
                .WithBlue((grp & 0x0F00) >> 8);
            handle.DrawLine(basisA, basisB, color);
            handle.DrawLine(basisC, basisD, color);
        }

        if (data.IsSpace)
            handle.DrawCircle(tileCentre, 0.15f, Color.Yellow);

        if (data.MapAtmosphere)
            handle.DrawCircle(tileCentre, 0.1f, Color.Orange);

        if (data.NoGrid)
            handle.DrawCircle(tileCentre, 0.05f, Color.Black);
    }

    private void CheckAndShowBlockDir(AtmosDebugOverlayData data, DrawingHandleWorld handle, AtmosDirection dir,
        Vector2 tileCentre)
    {
        if (!data.BlockDirection.HasFlag(dir))
            return;

        // Account for South being 0.
        var atmosAngle = dir.ToAngle() - Angle.FromDegrees(90);
        var atmosAngleOfs = atmosAngle.ToVec() * 0.45f;
        var atmosAngleOfsR90 = new Vector2(atmosAngleOfs.Y, -atmosAngleOfs.X);
        var basisA = tileCentre + atmosAngleOfs - atmosAngleOfsR90;
        var basisB = tileCentre + atmosAngleOfs + atmosAngleOfsR90;
        handle.DrawLine(basisA, basisB, Color.Azure);
    }

    private void DrawPressureDirection(
        DrawingHandleWorld handle,
        AtmosDirection d,
        Vector2 center,
        Color color)
    {
        // Account for South being 0.
        var atmosAngle = d.ToAngle() - Angle.FromDegrees(90);
        var atmosAngleOfs = atmosAngle.ToVec() * 0.4f;
        handle.DrawLine(center, center + atmosAngleOfs, color);
    }

    private void DrawTooltip(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var mousePos = _input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (_ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        var coords= viewport.PixelToMap(mousePos.Position);
        var box = Box2.CenteredAround(coords.Position, 3 * Vector2.One);
        GetGrids(coords.MapId, new Box2Rotated(box));

        foreach (var (grid, msg) in _grids)
        {
            var index = _map.WorldToTile(grid, grid, coords.Position);
            foreach (var data in msg.OverlayData)
            {
                if (data?.Indices == index)
                {
                    DrawTooltip(handle, mousePos.Position, data.Value);
                    return;
                }
            }
        }
    }

    private void DrawTooltip(DrawingHandleScreen handle, Vector2 pos, AtmosDebugOverlayData data)
    {
        var lineHeight = _font.GetLineHeight(1f);
        var offset  = new Vector2(0, lineHeight);

        var moles = data.Moles == null
            ? "No Air"
            : data.Moles.Sum().ToString();

        handle.DrawString(_font, pos, $"Moles: {moles}");
        pos += offset;
        handle.DrawString(_font, pos, $"Temp: {data.Temperature}");
        pos += offset;
        handle.DrawString(_font, pos, $"Excited: {data.InExcitedGroup?.ToString() ?? "None"}");
        pos += offset;
        handle.DrawString(_font, pos, $"Space: {data.IsSpace}");
        pos += offset;
        handle.DrawString(_font, pos, $"Map: {data.MapAtmosphere}");
        pos += offset;
        handle.DrawString(_font, pos, $"NoGrid: {data.NoGrid}");
    }

    private void GetGrids(MapId mapId, Box2Rotated box)
    {
        _grids.Clear();
        _mapManager.FindGridsIntersecting(mapId, box, ref _grids, (EntityUid uid, MapGridComponent grid,
            ref List<(Entity<MapGridComponent>, DebugMessage)> state) =>
        {
            if (_system.TileData.TryGetValue(uid, out var data))
                state.Add(((uid, grid), data));
            return true;
        });
    }
}
