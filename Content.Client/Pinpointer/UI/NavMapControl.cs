using System.Numerics;
using Content.Client.NPC;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Content.Shared.Power;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
public sealed class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedTransformSystem _transform;

    public EntityUid? MapUid;

    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<EntityCoordinates, (bool Visible, Color Color, Texture? Texture)> TrackedEntities = new();
    public Dictionary<CableType, bool> ShowCables = new Dictionary<CableType, bool>
    {
        [CableType.HighVoltage] = false,
        [CableType.MediumVoltage] = false,
        [CableType.Apc] = false,
    };

    private Vector2 _offset;
    private bool _draggin;
    private bool _recentering = false;
    private readonly float _recenterMinimum = 0.05f;
    private readonly Font _font;
    //private static readonly Color TileColor = new(30, 67, 30);
    private static readonly Color TileColor = new(30, 57, 67);
    private static readonly Color BeaconColor = Color.FromSrgb(TileColor.WithAlpha(0.8f));

    private List<CableData> _cableData = new List<CableData>()
    {
        new CableData(CableType.HighVoltage, Color.Orange),
        new CableData(CableType.MediumVoltage, Color.Yellow, new Vector2(-0.2f, -0.2f)),
        new CableData(CableType.Apc, Color.LimeGreen, new Vector2(0.2f, 0.2f)),
    };

    public Dictionary<Vector2i, NavMapChunkPowerCables> PowerCableChunks = new();

    // TODO: https://github.com/space-wizards/RobustToolbox/issues/3818
    private readonly Label _zoom = new()
    {
        VerticalAlignment = VAlignment.Top,
        Margin = new Thickness(8f, 8f),
    };

    private readonly Button _recenter = new()
    {
        Text = "Recentre",
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    public NavMapControl() : base(8f, 128f, 48f)
    {
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 16);

        RectClipContent = true;
        HorizontalExpand = true;
        VerticalExpand = true;

        var topPanel = new PanelContainer()
        {
            PanelOverride = new StyleBoxFlat()
            {
                BackgroundColor = StyleNano.ButtonColorContext.WithAlpha(1f),
                BorderColor = StyleNano.PanelDark
            },
            VerticalExpand = false,
            Children =
            {
                _zoom,
                _recenter,
            }
        };

        var topContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                topPanel,
                new Control()
                {
                    Name = "DrawingControl",
                    VerticalExpand = true,
                    Margin = new Thickness(5f, 5f)
                }
            }
        };

        AddChild(topContainer);
        topPanel.Measure(Vector2Helpers.Infinity);

        _recenter.OnPressed += args =>
        {
            _recentering = true;
        };
    }

    public void CenterToCoordinates(EntityCoordinates coordinates)
    {
        if (_entManager.TryGetComponent<PhysicsComponent>(MapUid, out var physics))
        {
            _offset = new Vector2(coordinates.X, coordinates.Y) - physics.LocalCenter;
        }
        _recenter.Disabled = false;
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

            if (_offset.LengthSquared() < _recenterMinimum)
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

        if (!_entManager.TryGetComponent<NavMapComponent>(MapUid, out var navMap) ||
            !_entManager.TryGetComponent<TransformComponent>(MapUid, out var xform) ||
            !_entManager.TryGetComponent<MapGridComponent>(MapUid, out var grid))
        {
            return;
        }

        var offset = _offset;
        //var lineColor = new Color(102, 217, 102);
        var lineColor = new Color(102, 164, 217);

        if (_entManager.TryGetComponent<PhysicsComponent>(MapUid, out var physics))
        {
            offset += physics.LocalCenter;
        }

        // Draw tiles
        if (_entManager.TryGetComponent<FixturesComponent>(MapUid, out var manager))
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

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts[..poly.VertexCount], TileColor);
            }
        }

        // Draw the wall data
        var area = new Box2(-WorldRange, -WorldRange, WorldRange + 1f, WorldRange + 1f).Translated(offset);
        var tileSize = new Vector2(grid.TileSize, -grid.TileSize);

        for (var x = Math.Floor(area.Left); x <= Math.Ceiling(area.Right); x += SharedNavMapSystem.ChunkSize * grid.TileSize)
        {
            for (var y = Math.Floor(area.Bottom); y <= Math.Ceiling(area.Top); y += SharedNavMapSystem.ChunkSize * grid.TileSize)
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

        // Draw cables
        for (var x = Math.Floor(area.Left); x <= Math.Ceiling(area.Right); x += SharedNavMapSystem.ChunkSize * grid.TileSize)
        {
            for (var y = Math.Floor(area.Bottom); y <= Math.Ceiling(area.Top); y += SharedNavMapSystem.ChunkSize * grid.TileSize)
            {
                var floored = new Vector2i((int) x, (int) y);

                var chunkOrigin = SharedMapSystem.GetChunkIndices(floored, SharedNavMapSystem.ChunkSize);

                if (!PowerCableChunks.TryGetValue(chunkOrigin, out var chunk))
                    continue;

                foreach (var datum in _cableData)
                {
                    if (!ShowCables[datum.CableType])
                        continue;

                    for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                    {
                        var value = (int) Math.Pow(2, i);
                        var mask = chunk.CableData[datum.CableType] & value;

                        if (mask == 0x0)
                            continue;

                        var relativeTile = SharedNavMapSystem.GetTile(mask);
                        var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize - offset;
                        var position = new Vector2(tile.X, -tile.Y);
                        NavMapChunkPowerCables? neighborChunk;
                        bool neighbor;

                        // Only check the north and east neighbors

                        // East
                        if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                        {
                            neighbor = PowerCableChunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                       (neighborChunk.CableData[datum.CableType] & SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                        }
                        else
                        {
                            var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                            neighbor = (chunk.CableData[datum.CableType] & flag) != 0x0;
                        }

                        if (neighbor)
                        {
                            if (WorldRange / WorldMaxRange < 0.5f)
                            {
                                var drawOffset = new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset;

                                var leftTop = new Vector2
                                    (Math.Min(position.X, (position + new Vector2(1f, 0f)).X) - 0.1f,
                                    Math.Min(position.Y, (position + new Vector2(1f, 0f)).Y) - 0.1f);

                                var rightBottom = new Vector2
                                    (Math.Max(position.X, (position + new Vector2(1f, 0f)).X) + 0.1f,
                                    Math.Max(position.Y, (position + new Vector2(1f, 0f)).Y) + 0.1f);

                                handle.DrawRect(new UIBox2(Scale(leftTop + drawOffset), Scale(rightBottom + drawOffset)), datum.Color);
                            }

                            else
                            {
                                handle.DrawLine
                                (Scale(position + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset),
                                Scale(position + new Vector2(1f, 0f) + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset),
                                datum.Color);
                            }
                        }

                        // North
                        if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                        {
                            neighbor = PowerCableChunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                          (neighborChunk.CableData[datum.CableType] & SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                        }
                        else
                        {
                            var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                            neighbor = (chunk.CableData[datum.CableType] & flag) != 0x0;
                        }

                        if (neighbor)
                        {
                            if (WorldRange / WorldMaxRange < 0.5f)
                            {
                                var drawOffset = new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset;

                                var leftTop = new Vector2
                                    (Math.Min(position.X, (position + new Vector2(0f, -1f)).X) - 0.1f,
                                    Math.Min(position.Y, (position + new Vector2(0f, -1f)).Y) - 0.1f);

                                var rightBottom = new Vector2
                                    (Math.Max(position.X, (position + new Vector2(0f, -1f)).X) + 0.1f,
                                    Math.Max(position.Y, (position + new Vector2(0f, -1f)).Y) + 0.1f);

                                handle.DrawRect(new UIBox2(Scale(leftTop + drawOffset), Scale(rightBottom + drawOffset)), datum.Color);
                            }

                            else
                            {
                                handle.DrawLine
                                (Scale(position + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset),
                                Scale(position + new Vector2(0f, -1f) + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f) + datum.Offset),
                                datum.Color);
                            }
                        }
                    }
                }
            }
        }

        var curTime = Timing.RealTime;
        var blinkFrequency = 1f / 1f;
        var lit = curTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;

        foreach (var (coord, value) in TrackedCoordinates)
        {
            if (lit && value.Visible)
            {
                var mapPos = coord.ToMap(_entManager);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = xform.InvWorldMatrix.Transform(mapPos.Position) - offset;
                    position = Scale(new Vector2(position.X, -position.Y));

                    handle.DrawCircle(position, float.Sqrt(MinimapScale) * 2f, value.Color);
                }
            }
        }

        foreach (var (coord, value) in TrackedEntities)
        {
            if (lit && value.Visible)
            {
                var mapPos = coord.ToMap(_entManager);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = xform.InvWorldMatrix.Transform(mapPos.Position) - offset;
                    position = Scale(new Vector2(position.X, -position.Y));

                    float f = 2.5f;
                    var rect = new UIBox2(position.X - float.Sqrt(MinimapScale) * f, position.Y - float.Sqrt(MinimapScale) * f, position.X + float.Sqrt(MinimapScale) * f, position.Y + float.Sqrt(MinimapScale) * f);

                    if (value.Texture != null)
                        handle.DrawTextureRect(value.Texture, rect, value.Color);
                    else
                        handle.DrawCircle(position, float.Sqrt(MinimapScale) * 2f, value.Color);
                }
            }
        }

        // Beacons
        var labelOffset = new Vector2(0.5f, 0.5f) * MinimapScale;
        var rectBuffer = new Vector2(5f, 3f);

        foreach (var beacon in navMap.Beacons)
        {
            var position = beacon.Position - offset;

            position = Scale(position with { Y = -position.Y });

            handle.DrawCircle(position, MinimapScale / 2f, beacon.Color);
            var textDimensions = handle.GetDimensions(_font, beacon.Text, 1f);

            var labelPosition = position + labelOffset;
            handle.DrawRect(new UIBox2(labelPosition, labelPosition + textDimensions + rectBuffer * 2), BeaconColor);
            handle.DrawString(_font, labelPosition + rectBuffer, beacon.Text, beacon.Color);
        }
    }

    private Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidpointVector;
    }
}

[DataDefinition]
public sealed partial class CableData
{
    public CableType CableType;
    public Color Color;
    public Vector2 Offset;

    public CableData(CableType cableType, Color color, Vector2 offset = new Vector2())
    {
        CableType = cableType;
        Color = color;
        Offset = offset;
    }
}
