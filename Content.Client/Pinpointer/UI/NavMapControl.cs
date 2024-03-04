using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
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
    [Dependency] private IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transformSystem;

    public EntityUid? Owner;
    public EntityUid? MapUid;

    protected override bool Draggable => true;

    // Actions
    public event Action<NetEntity?>? TrackedEntitySelectedAction;
    public event Action<DrawingHandleScreen>? PostWallDrawingAction;

    // Tracked data
    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<NetEntity, NavMapBlip> TrackedEntities = new();
    public Dictionary<Vector2i, List<NavMapLine>>? TileGrid = default!;

    // Default colors
    public Color WallColor = new(102, 217, 102);
    public Color TileColor = new(30, 67, 30);

    // Constants
    protected float UpdateTime = 1.0f;
    protected float MaxSelectableDistance = 10f;
    protected float MinDragDistance = 5f;
    protected static float MinDisplayedRange = 8f;
    protected static float MaxDisplayedRange = 128f;
    protected static float DefaultDisplayedRange = 48f;

    // Local variables
    private float _updateTimer = 0.25f;
    private Dictionary<Color, Color> _sRGBLookUp = new();
    protected Color BackgroundColor;
    protected float BackgroundOpacity = 0.9f;
    private int _targetFontsize = 8;

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
        Pressed = true,
    };

    public NavMapControl() : base(MinDisplayedRange, MaxDisplayedRange, DefaultDisplayedRange)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = EntManager.System<SharedTransformSystem>();
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

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
            Recentering = true;
        };

        ForceNavMapUpdate();
    }

    public void ForceNavMapUpdate()
    {
        EntManager.TryGetComponent(MapUid, out _navMap);
        EntManager.TryGetComponent(MapUid, out _grid);

        UpdateNavMap();
    }

    public void CenterToCoordinates(EntityCoordinates coordinates)
    {
        if (_physics != null)
            Offset = new Vector2(coordinates.X, coordinates.Y) - _physics.LocalCenter;

        _recenter.Disabled = false;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (TrackedEntitySelectedAction == null)
                return;

            if (_xform == null || _physics == null || TrackedEntities.Count == 0)
                return;

            // If the cursor has moved a significant distance, exit
            if ((StartDragPosition - args.PointerLocation.Position).Length() > MinDragDistance)
                return;

            // Get the clicked position
            var offset = Offset + _physics.LocalCenter;
            var localPosition = args.PointerLocation.Position - GlobalPixelPosition;

            // Convert to a world position
            var unscaledPosition = (localPosition - MidPointVector) / MinimapScale;
            var worldPosition = _transformSystem.GetWorldMatrix(_xform).Transform(new Vector2(unscaledPosition.X, -unscaledPosition.Y) + offset);

            // Find closest tracked entity in range
            var closestEntity = NetEntity.Invalid;
            var closestDistance = float.PositiveInfinity;

            foreach ((var currentEntity, var blip) in TrackedEntities)
            {
                if (!blip.Selectable)
                    continue;

                var currentDistance = (blip.Coordinates.ToMapPos(EntManager, _transformSystem) - worldPosition).Length();

                if (closestDistance < currentDistance || currentDistance * MinimapScale > MaxSelectableDistance)
                    continue;

                closestEntity = currentEntity;
                closestDistance = currentDistance;
            }

            if (closestDistance > MaxSelectableDistance || !closestEntity.IsValid())
                return;

            TrackedEntitySelectedAction.Invoke(closestEntity);
        }

        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            // Clear current selection with right click
            TrackedEntitySelectedAction?.Invoke(null);
        }

        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            // Toggle beacon labels
            _beacons.Pressed = !_beacons.Pressed;
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (Offset != Vector2.Zero)
            _recenter.Disabled = false;
        else
            _recenter.Disabled = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // Get the components necessary for drawing the navmap
        EntManager.TryGetComponent(MapUid, out _navMap);
        EntManager.TryGetComponent(MapUid, out _grid);
        EntManager.TryGetComponent(MapUid, out _xform);
        EntManager.TryGetComponent(MapUid, out _physics);
        EntManager.TryGetComponent(MapUid, out _fixtures);

        // Map re-centering
        _recenter.Disabled = DrawRecenter();

        _zoom.Text = Loc.GetString("navmap-zoom", ("value", $"{(DefaultDisplayedRange / WorldRange ):0.0}"));

        if (_navMap == null || _xform == null)
            return;

        var offset = Offset;

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

                    verts[i] = ScalePosition(new Vector2(vert.X, -vert.Y));
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
                    var start = ScalePosition(chunkedLine.Origin - new Vector2(offset.X, -offset.Y));
                    var end = ScalePosition(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y));

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

        var airlockBuffer = Vector2.One * (MinimapScale / 2.25f) * 0.75f;
        var airlockLines = new ValueList<Vector2>();
        var foobarVec = new Vector2(1, -1);

        foreach (var airlock in _navMap.Airlocks)
        {
            var position = airlock.Position - offset;
            position = ScalePosition(position with { Y = -position.Y });
            airlockLines.Add(position + airlockBuffer);
            airlockLines.Add(position - airlockBuffer * foobarVec);

            airlockLines.Add(position + airlockBuffer);
            airlockLines.Add(position + airlockBuffer * foobarVec);

            airlockLines.Add(position - airlockBuffer);
            airlockLines.Add(position + airlockBuffer * foobarVec);

            airlockLines.Add(position - airlockBuffer);
            airlockLines.Add(position - airlockBuffer * foobarVec);

            airlockLines.Add(position + airlockBuffer * -Vector2.UnitY);
            airlockLines.Add(position - airlockBuffer * -Vector2.UnitY);
        }

        if (airlockLines.Count > 0)
        {
            if (!_sRGBLookUp.TryGetValue(WallColor, out var sRGB))
            {
                sRGB = Color.ToSrgb(WallColor);
                _sRGBLookUp[WallColor] = sRGB;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, airlockLines.Span, sRGB);
        }

        if (PostWallDrawingAction != null)
            PostWallDrawingAction.Invoke(handle);

        // Beacons
        if (_beacons.Pressed)
        {
            var rectBuffer = new Vector2(5f, 3f);

            // Calculate font size for current zoom level
            var fontSize = (int) Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * _targetFontsize , 0);
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), fontSize);

            foreach (var beacon in _navMap.Beacons)
            {
                var position = beacon.Position - offset;
                position = ScalePosition(position with { Y = -position.Y });

                var textDimensions = handle.GetDimensions(font, beacon.Text, 1f);
                handle.DrawRect(new UIBox2(position - textDimensions / 2 - rectBuffer, position + textDimensions / 2 + rectBuffer), BackgroundColor);
                handle.DrawString(font, position - textDimensions / 2, beacon.Text, beacon.Color);
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
                var mapPos = coord.ToMap(EntManager, _transformSystem);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                    position = ScalePosition(new Vector2(position.X, -position.Y));

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

            var mapPos = blip.Coordinates.ToMap(EntManager, _transformSystem);

            if (mapPos.MapId != MapId.Nullspace)
            {
                var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                position = ScalePosition(new Vector2(position.X, -position.Y));

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

    protected Vector2 GetOffset()
    {
        return Offset + (_physics?.LocalCenter ?? new Vector2());
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
