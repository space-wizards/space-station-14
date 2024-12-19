using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;

namespace Content.Client.StationTeleporter;

public sealed partial class StationTeleporterNavMapControl : NavMapControl
{
    public HashSet<(Vector2, Vector2)> DrawLines = new();

    private readonly SharedTransformSystem _transformSystem;

    public StationTeleporterNavMapControl() : base()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();

        MaxSelectableDistance = 30f;

        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        PostWallDrawingAction += DrawAllTeleporterLinks;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var link in DrawLines) //TODO: Its work fine with all Maps expect Dev. Not sure why.
        {
            if (_xform is null)
                continue;

            var pos1 = Vector2.Transform(link.Item1, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos1 = ScalePosition(new Vector2(pos1.X, -pos1.Y));

            var pos2 = Vector2.Transform(link.Item2, _transformSystem.GetInvWorldMatrix(_xform)) - Offset;
            pos2 = ScalePosition(new Vector2(pos2.X, -pos2.Y));

            handle.DrawLine(pos1, pos2, Color.Aqua); //TODO: optimize Draw calls
        }
    }


    private void DrawAllTeleporterLinks(DrawingHandleScreen handle)
    {
    }
}
