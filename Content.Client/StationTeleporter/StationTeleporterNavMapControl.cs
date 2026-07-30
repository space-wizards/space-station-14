using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;

namespace Content.Client.StationTeleporter;

public sealed partial class StationTeleporterNavMapControl : NavMapControl
{
    private readonly HashSet<(Vector2, Vector2)> _linkedTeleportersCoordinates = new();

    private readonly SharedTransformSystem _transformSystem;

    public StationTeleporterNavMapControl()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();

        MaxSelectableDistance = 30f;

        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        PostWallDrawingAction += DrawAllTeleporterLinks;
    }

    public void AddTeleporterLink(Vector2 first, Vector2 second)
    {
        _linkedTeleportersCoordinates.Add((first, second));
    }

    public void ClearTeleporterLinks()
    {
        _linkedTeleportersCoordinates.Clear();
    }

    private void DrawAllTeleporterLinks(DrawingHandleScreen handle)
    {
        if (Xform is null)
            return;

        foreach (var link in _linkedTeleportersCoordinates)
        {
            var pos1 = Vector2.Transform(link.Item1, _transformSystem.GetInvWorldMatrix(Xform)) - GetOffset();
            pos1 = ScalePosition(new Vector2(pos1.X, -pos1.Y));

            var pos2 = Vector2.Transform(link.Item2, _transformSystem.GetInvWorldMatrix(Xform)) - GetOffset();
            pos2 = ScalePosition(new Vector2(pos2.X, -pos2.Y));

            handle.DrawLine(pos1, pos2, Color.Aqua);
        }
    }
}
