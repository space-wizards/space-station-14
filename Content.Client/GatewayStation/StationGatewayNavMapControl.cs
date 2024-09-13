using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.GatewayStation;

public sealed partial class StationGatewayNavMapControl : NavMapControl
{
    public NetEntity? Focus;
    public HashSet<GatewayLinkLine> LinkLines = new();

    private readonly SharedTransformSystem _transformSystem;
    public StationGatewayNavMapControl() : base()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();

        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Focus == null)
            return;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var link in LinkLines) //TODO: Its work fine with all Maps expect Dev. Not sure why.
        {
            var map1 = _transformSystem.ToMapCoordinates(link.Start);
            var map2 = _transformSystem.ToMapCoordinates(link.End);

            if (map1.MapId == MapId.Nullspace || map2.MapId == MapId.Nullspace)
                continue;

            if (map1.MapId != map2.MapId)
                continue;

            if (_xform is null)
                continue;

            var pos1 = Vector2.Transform(map1.Position, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos1 = ScalePosition(new Vector2(pos1.X, -pos1.Y));

            var pos2 = Vector2.Transform(map2.Position, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos2 = ScalePosition(new Vector2(pos2.X, -pos2.Y));

            handle.DrawLine(pos1, pos2, Color.Aqua); //TODO: optimize Draw calls
        }
    }
}

public struct GatewayLinkLine
{
    public readonly EntityCoordinates Start;
    public readonly EntityCoordinates End;

    public GatewayLinkLine(EntityCoordinates start, EntityCoordinates end)
    {
        Start = start;
        End = end;
    }
}
