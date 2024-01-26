using System.Numerics;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Handles the radar drawing part of the shuttle map.
/// </summary>
public sealed class ShuttleMapControl : BaseShuttleControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private SharedShuttleSystem _shuttles;
    private SharedTransformSystem _xformSystem;

    protected override bool Draggable => true;

    public MapId ViewingMap = MapId.Nullspace;

    private Font _font;
    private Texture _backgroundTexture;

    private List<Entity<MapGridComponent>> _grids = new();

    public ShuttleMapControl() : base(256f, 512f, 512f)
    {
        _shuttles = _entManager.System<SharedShuttleSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
        _backgroundTexture = cache.GetResource<TextureResource>("/Textures/Parallaxes/space_map2.png");
    }

    public void SetMap(MapId mapId, Vector2 offset)
    {
        ViewingMap = mapId;
        Offset = offset;
        Recentering = false;
    }

    protected void DrawDottedLine(DrawingHandleScreen screen, Vector2 from, Vector2 to, Color color, float offset = 0f, float dashSize = 8f, float gapSize = 2f)
    {
        var lineVector = (to - from);

        // No drawing for you.
        if (lineVector.LengthSquared() < 10f * float.Epsilon)
            return;

        var lineAndGap = gapSize + dashSize;
        var lines = new ValueList<Vector2>();

        // Minimum distance.
        if (lineVector.Length() < lineAndGap)
        {
            lines.Add(from);
            lines.Add(to);
        }
        else
        {
            var maxLength = lineVector.Length();
            var normalizedLine = lineVector.Normalized();
            var dashVector = normalizedLine * dashSize;
            var gapVector = normalizedLine * gapSize;

            var position = from;
            offset %= (dashSize + gapSize);
            var length = offset;
            var dashLength = dashSize;

            // If offset is less than gap size then start with a gap
            // otherwise start with a partial line
            if (offset > 0f)
            {
                if (offset < gapSize)
                {
                    position += normalizedLine * offset;
                    length += offset;
                }
                else
                {
                    dashLength = (offset - gapSize);
                }
            }

            while (length < maxLength)
            {
                lines.Add(position);

                position += normalizedLine * dashLength;
                var lengthFromStart = (position - from).Length();

                // if over length then cap the thing.
                if (lengthFromStart > maxLength)
                {
                    position = to;
                }

                lines.Add(position);
                dashLength = dashVector.Length();
                position += gapVector;
                length = (position - from).Length();
            }
        }

        screen.DrawPrimitives(DrawPrimitiveTopology.LineList, lines.Span, color);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (ViewingMap == MapId.Nullspace)
            return;

        // Draw background texture
        var tex = _backgroundTexture;

        // Size of the texture in world units.
        var size = tex.Size * MinimapScale * 1f;

        var position = ScalePosition(new Vector2(-Offset.X, Offset.Y));
        var slowness = 1f;

        // The "home" position is the effective origin of this layer.
        // Parallax shifting is relative to the home, and shifts away from the home and towards the Eye centre.
        // The effects of this are such that a slowness of 1 anchors the layer to the centre of the screen, while a slowness of 0 anchors the layer to the world.
        // (For values 0.0 to 1.0 this is in effect a lerp, but it's deliberately unclamped.)
        // The ParallaxAnchor adapts the parallax for station positioning and possibly map-specific tweaks.
        var home = Vector2.Zero;
        var scrolled = Vector2.Zero;

        // Origin - start with the parallax shift itself.
        var originBL = (position - home) * slowness + scrolled;

        // Place at the home.
        originBL += home;

        // Centre the image.
        originBL -= size / 2;

        // Remove offset so we can floor.
        var botLeft = new Vector2(0f, 0f);
        var topRight = botLeft + Size;

        var flooredBL = botLeft - originBL;

        // Floor to background size.
        flooredBL = (flooredBL / size).Floored() * size;

        // Re-offset.
        flooredBL += originBL;

        for (var x = flooredBL.X; x < topRight.X; x += size.X)
        {
            for (var y = flooredBL.Y; y < topRight.Y; y += size.Y)
            {
                handle.DrawTextureRect(tex, new UIBox2(x, y, x + size.X, y + size.Y));
            }
        }

        var viewBox = new Box2(Offset - WorldRangeVector, Offset + WorldRangeVector);

        // TODO: Do the edge drawing like radarcontrol for these.
        // TODO: Range drawing for nav computer.
        _grids.Clear();
        _mapManager.FindGridsIntersecting(ViewingMap, viewBox, ref _grids, approx: true, includeMap: false);
        var matty = Matrix3.CreateInverseTransform(Offset, Angle.Zero);
        var verts = new Dictionary<Color, List<Vector2>>();
        var edges = new Dictionary<Color, List<Vector2>>();
        var strings = new Dictionary<Color, List<(Vector2, string)>>();

        // Constant size diamonds
        var diamondRadius = WorldRange / 40f;

        // If mouse highlighting a grid then show a highlight and support clicks
        var inputManager = IoCManager.Resolve<IInputManager>();
        // inputManager.MouseScreenPosition.;

        // TODO:
        // Split each screen to its own control
        // Need a common method for buildmapobjects + drawing so they show same shit
        // Map objects looks good just need xaml
        // Maybe have the below as controls to get click support and hover
        // Need to open a special context menu for the grid (atm needs dock button)
        // Need per-map support for parallax textures (use component for path or default if none found), use nukies for planets
        // Remove the FTL stuff so it doesn't force rebuilds
        // need FTL button
        // Rebuild should play a ping.
        // Need pre-vis for shuttle arrival spot
        // Need circular buffers for FTL

        var realTime = IoCManager.Resolve<IGameTiming>().RealTime;

        foreach (var grid in _grids)
        {
            var gridColor = _shuttles.GetIFFColor(grid);

            var existingVerts = verts.GetOrNew(gridColor);
            var existingEdges = edges.GetOrNew(gridColor);

            var gridPos = _xformSystem.GetWorldPosition(grid.Owner);

            var gridRelativePos = matty.Transform(gridPos);
            gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
            var gridUiPos = ScalePosition(gridRelativePos);

            var bottom = ScalePosition(gridRelativePos + new Vector2(0f, -2f * diamondRadius));
            var right = ScalePosition(gridRelativePos + new Vector2(diamondRadius, 0f));
            var top = ScalePosition(gridRelativePos + new Vector2(0f, 2f * diamondRadius));
            var left = ScalePosition(gridRelativePos + new Vector2(-diamondRadius, 0f));

            var offset = (float) realTime.TotalSeconds * 30f;
            DrawDottedLine(handle, gridUiPos, MidPointVector, Color.Orange, offset: offset);

            // Diamond interior
            existingVerts.Add(bottom);
            existingVerts.Add(right);
            existingVerts.Add(top);

            existingVerts.Add(bottom);
            existingVerts.Add(top);
            existingVerts.Add(left);

            // Diamond edges
            existingEdges.Add(bottom);
            existingEdges.Add(right);
            existingEdges.Add(right);
            existingEdges.Add(top);
            existingEdges.Add(top);
            existingEdges.Add(left);
            existingEdges.Add(left);
            existingEdges.Add(bottom);

            // Text
            var iffText = _shuttles.GetIFFLabel(grid);

            if (string.IsNullOrEmpty(iffText))
                continue;

            var existingStrings = strings.GetOrNew(gridColor);
            existingStrings.Add((gridUiPos, iffText));
        }

        // Batch the colors whoopie
        // really only affects forks with lots of grids.
        foreach (var (color, sendVerts) in verts)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, sendVerts.ToArray(), color.WithAlpha(0.05f));
        }

        foreach (var (color, sendEdges) in edges)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, sendEdges.ToArray(), color);
        }

        foreach (var (color, sendStrings) in strings)
        {
            foreach (var (gridUiPos, iffText) in sendStrings)
            {
                var textWidth = handle.GetDimensions(_font, iffText, UIScale);
                handle.DrawString(_font, gridUiPos + new Vector2(-textWidth.X / 2f, textWidth.Y), iffText, color);
            }
        }
    }
}
