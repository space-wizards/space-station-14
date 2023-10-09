using Content.Client.Power;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
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
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
[UsedImplicitly]
public sealed partial class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public EntityUid? MapUid;

    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<EntityCoordinates, (bool Visible, Color Color, Texture? Texture)> TrackedEntities = new();
    public Dictionary<Vector2i, List<ChunkedLine>> PowerCableNetwork = default!;
    public Dictionary<Vector2i, List<ChunkedLine>>? FocusCableNetwork;
    public Dictionary<Vector2i, List<ChunkedLine>> TileGrid = default!;
    public Dictionary<CableType, bool> ShowCables = new Dictionary<CableType, bool>
    {
        [CableType.HighVoltage] = false,
        [CableType.MediumVoltage] = false,
        [CableType.Apc] = false,
    };
    public bool ShowBeacons = true;
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

    private List<CableData> _unfocusCableData = new List<CableData>()
    {
        new CableData(CableType.HighVoltage, new Color(82,52,0)),
        new CableData(CableType.MediumVoltage, new Color(80,80,0), new Vector2(-0.2f, -0.2f)),
        new CableData(CableType.Apc, new Color(20,76,20), new Vector2(0.2f, 0.2f)),
    };

    public Dictionary<Vector2i, NavMapChunkPowerCables> PowerCableChunks = new();
    public Dictionary<Vector2i, NavMapChunkPowerCables>? FocusPowerCableChunks = new();

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

        var area = new Box2(-WorldRange, -WorldRange, WorldRange + 1f, WorldRange + 1f).Translated(offset);

        if (TileGrid != null && TileGrid.Any())
        {
            foreach ((var chunk, var chunkedLines) in TileGrid)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    handle.DrawLine
                    (Scale(chunkedLine.Origin - new Vector2(offset.X, -offset.Y)),
                    Scale(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y)),
                    new Color(102, 164, 217));
                }
            }
        }

        if (PowerCableNetwork != null && PowerCableNetwork.Any())
        {
            foreach ((var chunk, var chunkedLines) in PowerCableNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    if (WorldRange / WorldMaxRange < 0.5f)
                    {
                        var leftTop = new Vector2
                            (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                            Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f);

                        var rightBottom = new Vector2
                            (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                            Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f);

                        handle.DrawRect
                            (new UIBox2(Scale(leftTop - new Vector2(offset.X, -offset.Y)),
                            Scale(rightBottom - new Vector2(offset.X, -offset.Y))),
                            chunkedLine.Color);
                    }

                    else
                    {
                        handle.DrawLine
                            (Scale(chunkedLine.Origin - new Vector2(offset.X, -offset.Y)),
                            Scale(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y)),
                            chunkedLine.Color);
                    }
                }
            }
        }

        if (FocusCableNetwork != null && FocusCableNetwork.Any())
        {
            foreach ((var chunk, var chunkedLines) in FocusCableNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    if (WorldRange / WorldMaxRange < 0.5f)
                    {
                        var leftTop = new Vector2
                            (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                            Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f);

                        var rightBottom = new Vector2
                            (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                            Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f);

                        handle.DrawRect
                            (new UIBox2(Scale(leftTop - new Vector2(offset.X, -offset.Y)),
                            Scale(rightBottom - new Vector2(offset.X, -offset.Y))),
                            chunkedLine.Color);
                    }

                    else
                    {
                        handle.DrawLine
                            (Scale(chunkedLine.Origin - new Vector2(offset.X, -offset.Y)),
                            Scale(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y)),
                            chunkedLine.Color);
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
        if (ShowBeacons)
        {
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
