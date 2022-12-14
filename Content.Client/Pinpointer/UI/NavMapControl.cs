using System.Globalization;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
public sealed class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public EntityUid? Uid;

    private Vector2 _offset;
    private bool _draggin;

    private bool _recentering = false;

    private float _recenterMinimum = 0.05f;

    private readonly Label _zoom = new()
    {
        VerticalAlignment = VAlignment.Top,
        Margin = new Thickness(4f, 2f),
    };

    private readonly Button _recenter = new()
    {
        Text = "Recentre",
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        Margin = new Thickness(4f, 2f),
        Disabled = true,
    };

    public NavMapControl() : base(8f, 128f, 48f)
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        AddChild(_zoom);
        AddChild(new Control());
        AddChild(_recenter);
        _recenter.OnPressed += args =>
        {
            _recentering = true;
        };
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.Use)
        {
            _draggin = true;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.Use)
        {
            _draggin = false;
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_draggin)
            return;

        _recentering = false;
        _offset -= new Vector2(args.Relative.X, -args.Relative.Y) / MidPoint * WorldRange;

        if (_offset != Vector2.Zero)
        {
            _recenter.Disabled = false;
        }
        else
        {
            _recenter.Disabled = true;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_recentering)
        {
            var frameTime = Timing.FrameTime;
            var diff = _offset * (float) frameTime.TotalSeconds;

            if (_offset.LengthSquared < _recenterMinimum)
            {
                _offset = Vector2.Zero;
                _recentering = false;
                _recenter.Disabled = true;
            }
            else
            {
                _offset -= diff * 5f;
            }
        }

        _zoom.Text = $"Zoom: {(WorldRange / WorldMaxRange * 100f):0.00}%";

        if (!_entManager.TryGetComponent<NavMapComponent>(Uid, out var navMap) ||
            !_entManager.TryGetComponent<TransformComponent>(Uid, out var xform) ||
            !_entManager.TryGetComponent<MapGridComponent>(Uid, out var grid))
        {
            return;
        }

        var offset = _offset;
        var tileColor = new Color(30, 67, 30);
        var lineColor = new Color(102, 217, 102);

        if (_entManager.TryGetComponent<PhysicsComponent>(Uid, out var physics))
        {
            offset += physics.LocalCenter;
        }

        // Draw tiles
        if (_entManager.TryGetComponent<FixturesComponent>(Uid, out var manager))
        {
            Span<Vector2> verts = new Vector2[8];

            foreach (var fixture in manager.Fixtures.Values)
            {
                if (fixture.Shape is not PolygonShape poly)
                    continue;

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    var vert = poly.Vertices[i] - offset;

                    verts[i] = Scale(new Vector2(vert.X, -vert.Y));
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts[..poly.VertexCount], tileColor);
            }
        }

        // Draw the wall data
        var area = new Box2(-WorldRange, -WorldRange, WorldRange, WorldRange).Translated(offset);
        var tileSize = new Vector2(grid.TileSize, -grid.TileSize);

        for (var x = Math.Floor(area.Left); x <= Math.Ceiling(area.Right + 1); x += SharedNavMapSystem.ChunkSize * grid.TileSize)
        {
            for (var y = Math.Floor(area.Bottom); y <= Math.Ceiling(area.Top + 1); y += SharedNavMapSystem.ChunkSize * grid.TileSize)
            {
                var floored = new Vector2i((int) x, (int) y);

                var chunkOrigin = SharedMapSystem.GetChunkIndices(floored, SharedNavMapSystem.ChunkSize);

                if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
                    continue;

                // TODO: Okay maybe I should just use ushorts lmao...
                for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                {
                    var value = (int) Math.Pow(2, i);

                    var mask = chunk.TileData & value;

                    if (mask == 0x0)
                        continue;

                    // Alright now we'll work out our edges
                    var relativeTile = SharedNavMapSystem.GetTile(mask);
                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize - offset;
                    var position = new Vector2(tile.X, -tile.Y);
                    NavMapChunk? neighborChunk;
                    bool neighbor;

                    // North edge
                    if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                      (neighborChunk.TileData &
                                       SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + new Vector2(0f, -grid.TileSize)), Scale(position + tileSize), lineColor);
                    }

                    // East edge
                    if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + tileSize), Scale(position + new Vector2(grid.TileSize, 0f)), lineColor);
                    }

                    // South edge
                    if (relativeTile.Y == 0)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(0, -1), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, SharedNavMapSystem.ChunkSize - 1))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, -1));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + new Vector2(grid.TileSize, 0f)), Scale(position), lineColor);
                    }

                    // West edge
                    if (relativeTile.X == 0)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(-1, 0), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(SharedNavMapSystem.ChunkSize - 1, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(-1, 0));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position), Scale(position + new Vector2(0f, -grid.TileSize)), lineColor);
                    }

                    // Draw a diagonal line for interiors.
                    handle.DrawLine(Scale(position + new Vector2(0f, -grid.TileSize)), Scale(position + new Vector2(grid.TileSize, 0f)), lineColor);
                }
            }
        }

        // TODO: Hacky bullshit
        var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
        var curTime = Timing.RealTime;
        var blinkFrequency = 1f / 1f;
        var lit = curTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;

        if (lit && _entManager.TryGetComponent<TransformComponent>(player, out var playerXform))
        {
            var position = xform.InvWorldMatrix.Transform(playerXform.WorldPosition) - offset;
            position = Scale(new Vector2(position.X, -position.Y));

            handle.DrawCircle(position, MinimapScale / 2f, Color.Red);
        }
    }

    private Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidPoint;
    }
}
