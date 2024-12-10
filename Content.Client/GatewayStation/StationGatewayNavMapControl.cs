using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;

namespace Content.Client.GatewayStation;

public sealed partial class StationGatewayNavMapControl : NavMapControl
{
    public HashSet<GatewayLinkLine> LinkLines = new();

    private readonly SharedTransformSystem _transformSystem;
    public StationGatewayNavMapControl() : base()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();

        MaxSelectableDistance = 30f;

        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var link in LinkLines) //TODO: Its work fine with all Maps expect Dev. Not sure why.
        {
            if (_xform is null)
                continue;

            var pos1 = Vector2.Transform(link.Start, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos1 = ScalePosition(new Vector2(pos1.X, -pos1.Y));

            var pos2 = Vector2.Transform(link.End, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos2 = ScalePosition(new Vector2(pos2.X, -pos2.Y));

            handle.DrawLine(pos1, pos2, Color.Aqua); //TODO: optimize Draw calls
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
