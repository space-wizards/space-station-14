using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using System.Numerics;
using JetBrains.Annotations;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
[UsedImplicitly, Virtual]
public partial class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedTransformSystem _transformSystem = default!;

    public EntityUid? Owner;
    public EntityUid? MapUid;

    // Actions
    public event Action<NetEntity?>? TrackedEntitySelectedAction;
    public event Action<DrawingHandleScreen>? PostWallDrawingAction;

    // Tracked data
    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<NetEntity, NavMapBlip> TrackedEntities = new();
    public Dictionary<Vector2i, List<NavMapLine>> TileGrid = default!;

    // Default colors
    public Color WallColor = new(102, 217, 102);
    public Color TileColor = new(30, 67, 30);

    // Constants
    protected float UpdateTime = 1.0f;
    protected float MaxSelectableDistance = 10f;
    protected float RecenterMinimum = 0.05f;
    protected float MinDragDistance = 5f;

    // Local variables
    private Vector2 _offset;
    private bool _draggin;
    private Vector2 _startDragPosition = default!;
    private bool _recentering = false;
    private readonly Font _font;
    private float _updateTimer = 0.25f;
    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();
    private Color _beaconColor;

    // Components
    private NavMapComponent? _navMap;
    private MapGridComponent? _grid;
    private TransformComponent? _xform;
    private PhysicsComponent? _physics;
    private FixturesComponent? _fixtures;

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
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 12);
        _beaconColor = Color.FromSrgb(TileColor.WithAlpha(0.8f));

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

        ForceNavMapUpdate();
    }

    public void ForceRecenter()
    {
        _recentering = true;
    }

    public void ForceNavMapUpdate()
    {
        _entManager.TryGetComponent(MapUid, out _navMap);
        _entManager.TryGetComponent(MapUid, out _grid);

        UpdateNavMap();
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
        {
            _startDragPosition = args.PointerLocation.Position;
            _draggin = true;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.Use)
            _draggin = false;

        if (TrackedEntitySelectedAction == null)
            return;

        if (args.Function == EngineKeyFunctions.Use)
        {
            if (_xform == null || _physics == null || TrackedEntities.Count == 0)
                return;

            // If the cursor has moved a significant distance, exit
            if ((_startDragPosition - args.PointerLocation.Position).Length() > MinDragDistance)
                return;

            // Get the clicked position
            var offset = _offset + _physics.LocalCenter;
            var localPosition = args.PointerLocation.Position - GlobalPixelPosition;

            // Convert to a world position
            var unscaledPosition = (localPosition - MidpointVector) / MinimapScale;
            var worldPosition = _transformSystem.GetWorldMatrix(_xform).Transform(new Vector2(unscaledPosition.X, -unscaledPosition.Y) + offset);

            // Find closest tracked entity in range
            var closestEntity = NetEntity.Invalid;
            var closestCoords = new EntityCoordinates();
            var closestDistance = float.PositiveInfinity;

            foreach ((var currentEntity, var blip) in TrackedEntities)
            {
                if (!blip.Selectable)
                    continue;

                var currentDistance = (blip.Coordinates.ToMapPos(_entManager, _transformSystem) - worldPosition).Length();

                if (closestDistance < currentDistance || currentDistance * MinimapScale > MaxSelectableDistance)
                    continue;

                closestEntity = currentEntity;
                closestCoords = blip.Coordinates;
                closestDistance = currentDistance;
            }

            if (closestDistance > MaxSelectableDistance || !closestEntity.IsValid())
                return;

            TrackedEntitySelectedAction.Invoke(closestEntity);
        }

        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            // Clear current selection with right click
            if (TrackedEntitySelectedAction != null)
                TrackedEntitySelectedAction.Invoke(null);
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
            _recenter.Disabled = false;

        else
            _recenter.Disabled = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // Get the components necessary for drawing the navmap 
        _entManager.TryGetComponent(MapUid, out _navMap);
        _entManager.TryGetComponent(MapUid, out _grid);
        _entManager.TryGetComponent(MapUid, out _xform);
        _entManager.TryGetComponent(MapUid, out _physics);
        _entManager.TryGetComponent(MapUid, out _fixtures);

        // Map re-centering
        if (_recentering)
        {
            var frameTime = Timing.FrameTime;
            var diff = _offset * (float) frameTime.TotalSeconds;

            if (_offset.LengthSquared() < RecenterMinimum)
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
        if (TileGrid != null && TileGrid.Count > 0)
        {
            var walls = new ValueList<Vector2>();

            foreach ((var chunk, var chunkedLines) in TileGrid)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    var start = Scale(chunkedLine.Origin - new Vector2(offset.X, -offset.Y));
                    var end = Scale(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y));

                    walls.Add(start);
                    walls.Add(end);
                }
            }

            if (walls.Count > 0)
            {
                if (!_sRGBLookUp.TryGetValue(WallColor, out var sRGB))
                {
                    sRGB = Color.ToSrgb(WallColor);
                    _sRGBLookUp[WallColor] = sRGB;
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, walls.Span, sRGB);
            }
        }

        if (PostWallDrawingAction != null)
            PostWallDrawingAction.Invoke(handle);

        // Beacons
        if (_beacons.Pressed)
        {
            var rectBuffer = new Vector2(5f, 3f);

            foreach (var beacon in _navMap.Beacons)
            {
                var position = beacon.Position - offset;
                position = Scale(position with { Y = -position.Y });

                var textDimensions = handle.GetDimensions(_font, beacon.Text, 1f);
                handle.DrawRect(new UIBox2(position - textDimensions / 2 - rectBuffer, position + textDimensions / 2 + rectBuffer), _beaconColor);
                handle.DrawString(_font, position - textDimensions / 2, beacon.Text, beacon.Color);
            }
        }

        var curTime = Timing.RealTime;
        var blinkFrequency = 1f / 1f;
        var lit = curTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;

        // Tracked coordinates (simple dot, legacy)
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

        // Tracked entities (can use a supplied sprite as a marker instead; should probably just replace TrackedCoordinates with this eventually)
        var iconVertexUVs = new Dictionary<(Texture, Color), ValueList<DrawVertexUV2D>>();

        foreach (var blip in TrackedEntities.Values)
        {
            if (blip.Blinks && !lit)
                continue;

            if (blip.Texture == null)
                continue;

            if (!iconVertexUVs.TryGetValue((blip.Texture, blip.Color), out var vertexUVs))
                vertexUVs = new();

            var mapPos = blip.Coordinates.ToMap(_entManager, _transformSystem);

            if (mapPos.MapId != MapId.Nullspace)
            {
                var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                position = Scale(new Vector2(position.X, -position.Y));

                var scalingCoefficient = 2.5f;
                var positionOffset = scalingCoefficient * float.Sqrt(MinimapScale);

                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X - positionOffset, position.Y - positionOffset), new Vector2(1f, 1f)));
                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X - positionOffset, position.Y + positionOffset), new Vector2(1f, 0f)));
                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X + positionOffset, position.Y - positionOffset), new Vector2(0f, 1f)));
                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X - positionOffset, position.Y + positionOffset), new Vector2(1f, 0f)));
                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X + positionOffset, position.Y - positionOffset), new Vector2(0f, 1f)));
                vertexUVs.Add(new DrawVertexUV2D(new Vector2(position.X + positionOffset, position.Y + positionOffset), new Vector2(0f, 0f)));
            }

            iconVertexUVs[(blip.Texture, blip.Color)] = vertexUVs;
        }

        foreach ((var (texture, color), var vertexUVs) in iconVertexUVs)
        {
            if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
            {
                sRGB = Color.ToSrgb(color);
                _sRGBLookUp[color] = sRGB;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, texture, vertexUVs.Span, sRGB);
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        // Update the timer
        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            UpdateNavMap();
        }
    }

    protected virtual void UpdateNavMap()
    {
        if (_navMap == null || _grid == null)
            return;

        TileGrid = GetDecodedWallChunks(_navMap.Chunks, _grid);
    }

    public Dictionary<Vector2i, List<NavMapLine>> GetDecodedWallChunks
        (Dictionary<Vector2i, NavMapChunk> chunks,
        MapGridComponent grid)
    {
        var decodedOutput = new Dictionary<Vector2i, List<NavMapLine>>();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<NavMapLine>();

            // TODO: Okay maybe I should just use ushorts lmao...
            for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
            {
                var value = (int) Math.Pow(2, i);

                var mask = chunk.TileData & value;

                if (mask == 0x0)
                    continue;

                // Alright now we'll work out our edges
                var relativeTile = SharedNavMapSystem.GetTile(mask);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize;
                var position = new Vector2(tile.X, -tile.Y);
                NavMapChunk? neighborChunk;
                bool neighbor;

                // North edge
                if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(0f, -grid.TileSize), position + new Vector2(grid.TileSize, -grid.TileSize)));
                }

                // East edge
                if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(grid.TileSize, -grid.TileSize), position + new Vector2(grid.TileSize, 0f)));
                }

                // South edge
                if (relativeTile.Y == 0)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, -1), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(grid.TileSize, 0f), position));
                }

                // West edge
                if (relativeTile.X == 0)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(-1, 0), out neighborChunk) &&
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
                    // Add point
                    list.Add(new NavMapLine(position, position + new Vector2(0f, -grid.TileSize)));
                }

                // Draw a diagonal line for interiors.
                list.Add(new NavMapLine(position + new Vector2(0f, -grid.TileSize), position + new Vector2(grid.TileSize, 0f)));
            }

            decodedOutput.Add(chunkOrigin, list);
        }

        return decodedOutput;
    }

    protected Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidpointVector;
    }

    protected Vector2 GetOffset()
    {
        return _offset + (_physics != null ? _physics.LocalCenter : new Vector2());
    }
}

public struct NavMapBlip
{
    public EntityCoordinates Coordinates;
    public Texture Texture;
    public Color Color;
    public bool Blinks;
    public bool Selectable;

    public NavMapBlip(EntityCoordinates coordinates, Texture texture, Color color, bool blinks, bool selectable = true)
    {
        Coordinates = coordinates;
        Texture = texture;
        Color = color;
        Blinks = blinks;
        Selectable = selectable;
    }
}

public struct NavMapLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;

    public NavMapLine(Vector2 origin, Vector2 terminus)
    {
        Origin = origin;
        Terminus = terminus;
    }
}
