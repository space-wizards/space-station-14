using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.GatewayStation;

public sealed partial class StationGatewayNavMapControl : NavMapControl
{
    public NetEntity? Focus;
    public HashSet<GatewayLinkLine> LinkLines = new();
    public StationGatewayNavMapControl() : base()
    {
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

        foreach (var link in LinkLines)
        {
            handle.DrawDottedLine(link.Start, link.End, Color.Aqua);
        }
    }
}

public struct GatewayLinkLine
{
    public readonly Vector2 Start;
    public readonly Vector2 End;

    public GatewayLinkLine(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }
}
