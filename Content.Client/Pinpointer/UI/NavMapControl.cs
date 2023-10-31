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
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using Content.Shared.Power;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
[UsedImplicitly]
public sealed partial class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedTransformSystem _transformSystem = default!;
    private readonly SpriteSystem _spriteSystem;

    public EntityUid? MapUid;
    public PowerMonitoringConsoleComponent? PowerMonitoringConsole;

    // Tracked data
    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<EntityCoordinates, NavMapTrackableComponent> TrackedEntities = new();
    public Dictionary<Vector2i, List<NavMapLine>> PowerCableNetwork = default!;
    public Dictionary<Vector2i, List<NavMapLine>>? FocusCableNetwork;
    public Dictionary<Vector2i, List<NavMapLine>> TileGrid = default!;

    // Default colors
    public Color WallColor = new(102, 217, 102);
    public Color TileColor = new(30, 67, 30);

    // Toggles
    public List<NavMapLineGroup> HiddenLineGroups = new List<NavMapLineGroup>();

    // Local
    private NavMapComponent? _navMap;
    private MapGridComponent? _grid;
    private TransformComponent? _xform;
    private PhysicsComponent? _physics;
    private FixturesComponent? _fixtures;

    private Vector2 _offset;
    private bool _draggin;
    private bool _recentering = false;
    private readonly float _recenterMinimum = 0.05f;
    private readonly Font _font;
    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    // TODO: https://github.com/space-wizards/RobustToolbox/issues/3818
    private readonly Label _zoom = new()
    {
        VerticalAlignment = VAlignment.Top,
        Margin = new Thickness(8f, 8f),
    };

    private readonly Button _recenter = new()
    {
        Text = Loc.GetString("navmap-recenter"),
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    private readonly CheckBox _beacons = new()
    {
        Text = Loc.GetString("navmap-toggle-beacons"),
        Margin = new Thickness(4f, 0f),
        VerticalAlignment = VAlignment.Center,
        HorizontalAlignment = HAlignment.Center,
        Pressed = false,
    };

    public NavMapControl() : base(8f, 128f, 48f)
    {
        IoCManager.InjectDependencies(this);
        var cache = IoCManager.Resolve<IResourceCache>();

        _transformSystem = _entManager.System<SharedTransformSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 12);

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
                _beacons,
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

    public void ForceRecenter()
    {
        _recentering = true;
    }

    public void CenterToCoordinates(EntityCoordinates coordinates)
    {
        if (_physics != null)
            _offset = new Vector2(coordinates.X, coordinates.Y) - _physics.LocalCenter;

        _recenter.Disabled = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.Use)
            _draggin = true;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.Use)
            _draggin = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_draggin)
            return;

        _recentering = false;
        _offset -= new Vector2(args.Relative.X, -args.Relative.Y) / MidPoint * WorldRange;

        if (_offset != Vector2.Zero)
            _recenter.Disabled = false;

        else
            _recenter.Disabled = true;
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

        _zoom.Text = Loc.GetString("navmap-zoom", ("value", $"{(WorldRange / WorldMaxRange * 100f):0.00}"));

        if (_navMap == null || _xform == null)
            return;

        var offset = _offset;

        if (_physics != null)
            offset += _physics.LocalCenter;

        // Draw tiles
        if (_fixtures != null)
        {
            Span<Vector2> verts = new Vector2[8];

            foreach (var fixture in _fixtures.Fixtures.Values)
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

        // Drawing lines can be rather expensive due to the number of neighbors that need to be checked in order  
        // to figure out where they should be drawn. However, we don't *need* to do check these every frame.
        // Instead, lets periodically update where to draw each line and then store these points in a list.
        // Then we can just run through the list each frame and draw the lines without any extra computation.

        // Draw walls
        if (TileGrid != null && TileGrid.Any() && !HiddenLineGroups.Contains(NavMapLineGroup.Wall))
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
                    chunkedLine.Color);
                }
            }
        }

        // Draw full cable network
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
                    if (HiddenLineGroups.Contains(chunkedLine.Group))
                        continue;

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

        // Draw focus network
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
                    if (HiddenLineGroups.Contains(chunkedLine.Group))
                        continue;

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

        // Tracked coordinates (simple dot)
        foreach (var (coord, value) in TrackedCoordinates)
        {
            if (lit && value.Visible)
            {
                var mapPos = coord.ToMap(_entManager, _transformSystem);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                    position = Scale(new Vector2(position.X, -position.Y));

                    handle.DrawCircle(position, float.Sqrt(MinimapScale) * 2f, value.Color);
                }
            }
        }

        // Tracked entities (can use a supplied sprite as a marker instead; should probably just replace TrackedCoordinates with this)
        foreach (var (coord, value) in TrackedEntities)
        {
            if ((lit || !value.Blinks) && value.Visible)
            {
                var mapPos = coord.ToMap(_entManager, _transformSystem);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                    position = Scale(new Vector2(position.X, -position.Y));

                    var scalingCoefficient = 2.5f;
                    var positionOffset = scalingCoefficient * float.Sqrt(MinimapScale);

                    var rect = new UIBox2
                        (position.X - positionOffset,
                        position.Y - positionOffset,
                        position.X + positionOffset,
                        position.Y + positionOffset);

                    if (value.Texture != null)
                        handle.DrawTextureRect(_spriteSystem.Frame0(value.Texture), rect, value.Color);
                    else
                        handle.DrawCircle(position, float.Sqrt(MinimapScale) * 2f, value.Color);
                }
            }
        }

        // Beacons
        if (_beacons.Pressed)
        {
            var labelOffset = new Vector2(0.5f, 0.5f) * MinimapScale;
            var rectBuffer = new Vector2(5f, 3f);
            var beaconColor = Color.FromSrgb(TileColor.WithAlpha(0.8f));

            foreach (var beacon in _navMap.Beacons)
            {
                var position = beacon.Position - offset;

                position = Scale(position with { Y = -position.Y });

                handle.DrawCircle(position, MinimapScale / 2f, beacon.Color);
                var textDimensions = handle.GetDimensions(_font, beacon.Text, 1f);

                var labelPosition = position + labelOffset;
                handle.DrawRect(new UIBox2(labelPosition, labelPosition + textDimensions + rectBuffer * 2), beaconColor);
                handle.DrawString(_font, labelPosition + rectBuffer, beacon.Text, beacon.Color);
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        // Get the components necessary for drawing the navmap 
        // Let's do that here so they only get checked once per frame
        _entManager.TryGetComponent(MapUid, out _navMap);
        _entManager.TryGetComponent(MapUid, out _grid);
        _entManager.TryGetComponent(MapUid, out _xform);
        _entManager.TryGetComponent(MapUid, out _physics);
        _entManager.TryGetComponent(MapUid, out _fixtures);

        // Update the timer
        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            if (_navMap == null || _grid == null)
                return;

            TileGrid = GetDecodedTileChunks(_navMap.Chunks, _grid);
            UpdatePowerCableChunks();
        }
    }

    public void UpdatePowerCableChunks()
    {
        if (PowerMonitoringConsole != null && _grid != null)
        {
            FocusCableNetwork = GetDecodedPowerCableChunks(PowerMonitoringConsole.FocusChunks, _grid);
            PowerCableNetwork = GetDecodedPowerCableChunks(PowerMonitoringConsole.AllChunks, _grid, PowerMonitoringConsole.Focus != null);
        }
    }

    private Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidpointVector;
    }
}
