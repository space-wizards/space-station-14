using System.Numerics;
using Content.Shared._NF.Radar;
using Robust.Client.Graphics;

//purposeful collision; this is a partial class
namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleNavControl
{

    private void DrawBlips(DrawingHandleScreen handle, Matrix3x2 worldToShuttle, Matrix3x2 shuttleToView)
    {
        //get the list of blips
        var rawBlips = _blipSystem.GetRawBlips();
        // Prepare view bounds for culling
        var blipViewBounds = new Box2(-3f, -3f, Size.X + 3f, Size.Y + 3f);

        // Draw blips using the same grid-relative transformation approach as docks
        foreach (var blip in rawBlips)
        {
            var blipPosInView = Vector2.Transform(blip.Position, worldToShuttle * shuttleToView);

            // Check if this blip is within view bounds before drawing
            if (blipViewBounds.Contains(blipPosInView))
            {
                DrawBlipShape(handle, blipPosInView, blip.Scale * 3f, blip.Color.WithAlpha(0.8f), blip.Shape);
            }
        }
    }

    private void DrawBlipShape(DrawingHandleScreen handle, Vector2 position, float size, Color color, RadarBlipShape shape)
    {
        switch (shape)
        {
            case RadarBlipShape.Circle:
                handle.DrawCircle(position, size, color);
                break;
            case RadarBlipShape.Square:
                var halfSize = size / 2;
                var rect = new UIBox2(
                    position.X - halfSize,
                    position.Y - halfSize,
                    position.X + halfSize,
                    position.Y + halfSize
                );
                handle.DrawRect(rect, color);
                break;
            case RadarBlipShape.Triangle:
                var points = new Vector2[]
                {
                position + new Vector2(0, -size),
                position + new Vector2(-size * 0.866f, size * 0.5f),
                position + new Vector2(size * 0.866f, size * 0.5f)
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, points, color);
                break;
            case RadarBlipShape.Star:
                DrawStar(handle, position, size, color);
                break;
            case RadarBlipShape.Diamond:
                var diamondPoints = new Vector2[]
                {
                position + new Vector2(0, -size),
                position + new Vector2(size, 0),
                position + new Vector2(0, size),
                position + new Vector2(-size, 0)
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, diamondPoints, color);
                break;
            case RadarBlipShape.Hexagon:
                DrawHexagon(handle, position, size, color);
                break;
            case RadarBlipShape.Arrow:
                DrawArrow(handle, position, size, color);
                break;
        }
    }

    private void DrawStar(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        const int points = 5;
        const float innerRatio = 0.4f;
        var vertices = new Vector2[points * 2 + 2]; // outer and inner point, five times, plus a center point and the original drawn point

        vertices[0] = position;
        for (var i = 0; i <= points * 2; i++)
        {
            var angle = i * Math.PI / points;
            var radius = i % 2 == 0 ? size : size * innerRatio;
            vertices[i + 1] = position + new Vector2(
                (float)Math.Sin(angle) * radius,
                -(float)Math.Cos(angle) * radius
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private void DrawHexagon(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var vertices = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Math.PI / 3;
            vertices[i] = position + new Vector2(
                (float)Math.Sin(angle) * size,
                -(float)Math.Cos(angle) * size
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private void DrawArrow(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var vertices = new Vector2[]
        {
        position + new Vector2(0, -size),           // Tip
        position + new Vector2(-size * 0.5f, 0),    // Left wing
        position + new Vector2(0, size * 0.5f),     // Bottom
        position + new Vector2(size * 0.5f, 0)      // Right wing
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }
}
