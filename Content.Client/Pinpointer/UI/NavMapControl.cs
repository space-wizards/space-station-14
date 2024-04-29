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
using Content.Shared.Atmos;
using System.Linq;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
[UsedImplicitly, Virtual]
public partial class NavMapControl : MapGridControl
{
    [Dependency] private IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedNavMapSystem _navMapSystem;

    public EntityUid? Owner;
    public EntityUid? MapUid;

    protected override bool Draggable => true;

    // Actions
    public event Action<NetEntity?>? TrackedEntitySelectedAction;
    public event Action<DrawingHandleScreen>? PostWallDrawingAction;

    // Tracked data
    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<NetEntity, NavMapBlip> TrackedEntities = new();

    public List<(Vector2, Vector2)> TileLines = new();
    public List<(Vector2, Vector2)> TileRects = new();
    public List<(Vector2[], Color)> TilePolygons = new();

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
    protected float MinmapScaleModifier = 0.075f;
    protected float FullWallInstep = 0.165f;
    protected float ThinWallThickness = 0.165f;
    protected float ThinDoorThickness = 0.30f;

    // Local variables
    private float _updateTimer = 1.0f;
    private Dictionary<Color, Color> _sRGBLookUp = new();
    protected Color BackgroundColor;
    protected float BackgroundOpacity = 0.9f;
    private int _targetFontsize = 8;

    protected Dictionary<(int, Vector2i), (int, Vector2i)> HorizLinesLookup = new();
    protected Dictionary<(int, Vector2i), (int, Vector2i)> HorizLinesLookupReversed = new();
    protected Dictionary<(int, Vector2i), (int, Vector2i)> VertLinesLookup = new();
    protected Dictionary<(int, Vector2i), (int, Vector2i)> VertLinesLookupReversed = new();

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
        HorizontalExpand = true,
        Margin = new Thickness(8f, 8f),
    };

    private readonly Button _recenter = new()
    {
        Text = Loc.GetString("navmap-recenter"),
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        HorizontalExpand = true,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    private readonly CheckBox _beacons = new()
    {
        Text = Loc.GetString("navmap-toggle-beacons"),
        VerticalAlignment = VAlignment.Center,
        HorizontalAlignment = HAlignment.Center,
        HorizontalExpand = true,
        Margin = new Thickness(4f, 0f),
        Pressed = true,
    };

    public NavMapControl() : base(MinDisplayedRange, MaxDisplayedRange, DefaultDisplayedRange)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = EntManager.System<SharedTransformSystem>();
        _navMapSystem = EntManager.System<SharedNavMapSystem>();

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
            HorizontalExpand = true,
            SetWidth = 650f,
            Children =
            {
                new BoxContainer()
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children =
                    {
                        _zoom,
                        _beacons,
                        _recenter
                    }
                }
            }
        };

        var topContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
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
        EntManager.TryGetComponent(MapUid, out _xform);
        EntManager.TryGetComponent(MapUid, out _physics);
        EntManager.TryGetComponent(MapUid, out _fixtures);

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

        if (_navMap == null || _grid == null || _xform == null)
            return;

        // Map re-centering
        _recenter.Disabled = DrawRecenter();

        // Update zoom text
        _zoom.Text = Loc.GetString("navmap-zoom", ("value", $"{(DefaultDisplayedRange / WorldRange):0.0}"));

        // Update offset with physics local center
        var offset = Offset;

        if (_physics != null)
            offset += _physics.LocalCenter;

        var offsetVec = new Vector2(offset.X, -offset.Y);

        // Wall sRGB
        if (!_sRGBLookUp.TryGetValue(WallColor, out var wallsRGB))
        {
            wallsRGB = Color.ToSrgb(WallColor);
            _sRGBLookUp[WallColor] = wallsRGB;
        }

        // Draw floor tiles
        if (TilePolygons.Any())
        {
            Span<Vector2> verts = new Vector2[8];

            foreach (var (polygonVerts, polygonColor) in TilePolygons)
            {
                for (var i = 0; i < polygonVerts.Length; i++)
                {
                    var vert = polygonVerts[i] - offset;
                    verts[i] = ScalePosition(new Vector2(vert.X, -vert.Y));
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts[..polygonVerts.Length], polygonColor);
            }
        }

        // Draw map lines
        if (TileLines.Any())
        {
            var lines = new ValueList<Vector2>(TileLines.Count * 2);

            foreach (var (o, t) in TileLines)
            {
                var origin = ScalePosition(o - offsetVec);
                var terminus = ScalePosition(t - offsetVec);

                lines.Add(origin);
                lines.Add(terminus);
            }

            if (lines.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, lines.Span, wallsRGB);
        }

        // Draw map rects
        if (TileRects.Any())
        {
            var rects = new ValueList<Vector2>(TileRects.Count * 8);

            foreach (var (lt, rb) in TileRects)
            {
                var leftTop = ScalePosition(lt - offsetVec);
                var rightBottom = ScalePosition(rb - offsetVec);

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                rects.Add(leftTop);
                rects.Add(rightTop);
                rects.Add(rightTop);
                rects.Add(rightBottom);
                rects.Add(rightBottom);
                rects.Add(leftBottom);
                rects.Add(leftBottom);
                rects.Add(leftTop);
            }

            if (rects.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, rects.Span, wallsRGB);
        }

        // Invoke post wall drawing action
        if (PostWallDrawingAction != null)
            PostWallDrawingAction.Invoke(handle);

        // Beacons
        if (_beacons.Pressed)
        {
            var rectBuffer = new Vector2(5f, 3f);

            // Calculate font size for current zoom level
            var fontSize = (int) Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * _targetFontsize, 0);
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
        foreach (var blip in TrackedEntities.Values)
        {
            if (blip.Blinks && !lit)
                continue;

            if (blip.Texture == null)
                continue;

            var mapPos = blip.Coordinates.ToMap(EntManager, _transformSystem);

            if (mapPos.MapId != MapId.Nullspace)
            {
                var position = _transformSystem.GetInvWorldMatrix(_xform).Transform(mapPos.Position) - offset;
                position = ScalePosition(new Vector2(position.X, -position.Y));

                var scalingCoefficient = MinmapScaleModifier * float.Sqrt(MinimapScale);
                var positionOffset = new Vector2(scalingCoefficient * blip.Texture.Width, scalingCoefficient * blip.Texture.Height);

                handle.DrawTextureRect(blip.Texture, new UIBox2(position - positionOffset, position + positionOffset), blip.Color);
            }
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
        // Clear stale values
        TilePolygons.Clear();
        TileLines.Clear();
        TileRects.Clear();

        UpdateNavMapFloorTiles();
        UpdateNavMapWallLines();
        UpdateNavMapAirlocks();
    }

    private void UpdateNavMapFloorTiles()
    {
        if (_fixtures == null)
            return;

        var verts = new Vector2[8];

        foreach (var fixture in _fixtures.Fixtures.Values)
        {
            if (fixture.Shape is not PolygonShape poly)
                continue;

            for (var i = 0; i < poly.VertexCount; i++)
            {
                var vert = poly.Vertices[i];
                verts[i] = new Vector2(MathF.Round(vert.X), MathF.Round(vert.Y));
            }

            TilePolygons.Add((verts[..poly.VertexCount], TileColor));
        }
    }

    private void UpdateNavMapWallLines()
    {
        if (_navMap == null || _grid == null)
            return;

        // We'll use the following dictionaries to combine collinear wall lines
        HorizLinesLookup.Clear();
        HorizLinesLookupReversed.Clear();
        VertLinesLookup.Clear();
        VertLinesLookupReversed.Clear();

        foreach ((var (category, chunkOrigin), var chunk) in _navMap.Chunks)
        {
            if (category != NavMapChunkType.Wall)
                continue;

            for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
            {
                var value = (ushort) Math.Pow(2, i);
                var mask = _navMapSystem.GetCombinedEdgesForChunk(chunk.TileData) & value;

                if (mask == 0x0)
                    continue;

                var relativeTile = SharedNavMapSystem.GetTile(mask);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * _grid.TileSize;

                if (!_navMapSystem.AllTileEdgesAreOccupied(chunk.TileData, relativeTile))
                {
                    AddRectForThinWall(chunk.TileData, tile);
                    continue;
                }

                tile = tile with { Y = -tile.Y };

                NavMapChunk? neighborChunk;
                bool neighbor;

                // North edge
                if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = _navMap.Chunks.TryGetValue((NavMapChunkType.Wall, chunkOrigin + new Vector2i(0, 1)), out neighborChunk) &&
                                  (neighborChunk.TileData[AtmosDirection.South] &
                                   SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                }
                else
                {
                    var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                    neighbor = (chunk.TileData[AtmosDirection.South] & flag) != 0x0;
                }

                if (!neighbor)
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize), tile + new Vector2i(_grid.TileSize, -_grid.TileSize), HorizLinesLookup, HorizLinesLookupReversed);

                // East edge
                if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = _navMap.Chunks.TryGetValue((NavMapChunkType.Wall, chunkOrigin + new Vector2i(1, 0)), out neighborChunk) &&
                               (neighborChunk.TileData[AtmosDirection.West] &
                                SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                }
                else
                {
                    var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                    neighbor = (chunk.TileData[AtmosDirection.West] & flag) != 0x0;
                }

                if (!neighbor)
                    AddOrUpdateNavMapLine(tile + new Vector2i(_grid.TileSize, -_grid.TileSize), tile + new Vector2i(_grid.TileSize, 0), VertLinesLookup, VertLinesLookupReversed);

                // South edge
                if (relativeTile.Y == 0)
                {
                    neighbor = _navMap.Chunks.TryGetValue((NavMapChunkType.Wall, chunkOrigin + new Vector2i(0, -1)), out neighborChunk) &&
                               (neighborChunk.TileData[AtmosDirection.North] &
                                SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, SharedNavMapSystem.ChunkSize - 1))) != 0x0;
                }
                else
                {
                    var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, -1));
                    neighbor = (chunk.TileData[AtmosDirection.North] & flag) != 0x0;
                }

                if (!neighbor)
                    AddOrUpdateNavMapLine(tile, tile + new Vector2i(_grid.TileSize, 0), HorizLinesLookup, HorizLinesLookupReversed);

                // West edge
                if (relativeTile.X == 0)
                {
                    neighbor = _navMap.Chunks.TryGetValue((NavMapChunkType.Wall, chunkOrigin + new Vector2i(-1, 0)), out neighborChunk) &&
                               (neighborChunk.TileData[AtmosDirection.East] &
                                SharedNavMapSystem.GetFlag(new Vector2i(SharedNavMapSystem.ChunkSize - 1, relativeTile.Y))) != 0x0;
                }
                else
                {
                    var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(-1, 0));
                    neighbor = (chunk.TileData[AtmosDirection.East] & flag) != 0x0;
                }

                if (!neighbor)
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize), tile, VertLinesLookup, VertLinesLookupReversed);

                // Add a diagonal line for interiors. Unless there are a lot of double walls, there is no point combining these
                TileLines.Add((tile + new Vector2(0, -_grid.TileSize), tile + new Vector2(_grid.TileSize, 0)));
            }
        }

        // Record the combined lines 
        foreach (var (origin, terminal) in HorizLinesLookup)
            TileLines.Add((origin.Item2, terminal.Item2));

        foreach (var (origin, terminal) in VertLinesLookup)
            TileLines.Add((origin.Item2, terminal.Item2));
    }

    private void UpdateNavMapAirlocks()
    {
        if (_navMap == null || _grid == null)
            return;

        foreach (var ((category, _), chunk) in _navMap.Chunks)
        {
            if (category != NavMapChunkType.Airlock)
                continue;

            for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
            {
                var value = (int) Math.Pow(2, i);
                var mask = _navMapSystem.GetCombinedEdgesForChunk(chunk.TileData) & value;

                if (mask == 0x0)
                    continue;

                var relative = SharedNavMapSystem.GetTile(mask);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relative) * _grid.TileSize;

                // If the edges of an airlock tile are not all occupied, draw a thin airlock for each edge
                if (!_navMapSystem.AllTileEdgesAreOccupied(chunk.TileData, relative))
                {
                    AddRectForThinAirlock(chunk.TileData, tile);
                    continue;
                }

                // Otherwise add a single full tile airlock
                TileRects.Add((new Vector2(tile.X + FullWallInstep, -tile.Y - FullWallInstep),
                    new Vector2(tile.X - FullWallInstep + 1f, -tile.Y + FullWallInstep - 1)));

                TileLines.Add((new Vector2(tile.X + 0.5f, -tile.Y - FullWallInstep),
                    new Vector2(tile.X + 0.5f, -tile.Y + FullWallInstep - 1)));
            }
        }
    }

    private void AddRectForThinWall(Dictionary<AtmosDirection, ushort> tileData, Vector2i tile)
    {
        if (_navMapSystem == null || _grid == null)
            return;

        var leftTop = new Vector2(-0.5f, -0.5f + ThinWallThickness);
        var rightBottom = new Vector2(0.5f, -0.5f);

        foreach (var (direction, mask) in tileData)
        {
            var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
            var flag = (ushort) SharedNavMapSystem.GetFlag(relative);

            if ((mask & flag) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);
            var angle = new Angle(0);

            switch (direction)
            {
                case AtmosDirection.East: angle = new Angle(MathF.PI * 0.5f); break;
                case AtmosDirection.South: angle = new Angle(MathF.PI); break;
                case AtmosDirection.West: angle = new Angle(MathF.PI * -0.5f); break;
            }

            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
        }
    }

    private void AddRectForThinAirlock(Dictionary<AtmosDirection, ushort> tileData, Vector2i tile)
    {
        if (_navMapSystem == null || _grid == null)
            return;

        var leftTop = new Vector2(-0.5f + FullWallInstep, -0.5f + FullWallInstep + ThinDoorThickness);
        var rightBottom = new Vector2(0.5f - FullWallInstep, -0.5f + FullWallInstep);
        var centreTop = new Vector2(0f, -0.5f + FullWallInstep + ThinDoorThickness);
        var centreBottom = new Vector2(0f, -0.5f + FullWallInstep);

        foreach (var (direction, mask) in tileData)
        {
            var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
            var flag = (ushort) SharedNavMapSystem.GetFlag(relative);

            if ((mask & flag) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);
            var angle = new Angle(0);

            switch (direction)
            {
                case AtmosDirection.East: angle = new Angle(MathF.PI * 0.5f);break;
                case AtmosDirection.South: angle = new Angle(MathF.PI); break;
                case AtmosDirection.West: angle = new Angle(MathF.PI * -0.5f); break;
            }

            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
            TileLines.Add((angle.RotateVec(centreTop) + tilePosition, angle.RotateVec(centreBottom) + tilePosition));
        }
    }

    protected void AddOrUpdateNavMapLine
        (Vector2i origin,
        Vector2i terminus,
        Dictionary<(int, Vector2i), (int, Vector2i)> lookup,
        Dictionary<(int, Vector2i), (int, Vector2i)> lookupReversed,
        int index = 0)
    {
        (int, Vector2i) foundTermiusTuple;
        (int, Vector2i) foundOriginTuple;

        if (lookup.TryGetValue((index, terminus), out foundTermiusTuple) &&
            lookupReversed.TryGetValue((index, origin), out foundOriginTuple))
        {
            lookup[foundOriginTuple] = foundTermiusTuple;
            lookupReversed[foundTermiusTuple] = foundOriginTuple;

            lookup.Remove((index, terminus));
            lookupReversed.Remove((index, origin));
        }

        else if (lookup.TryGetValue((index, terminus), out foundTermiusTuple))
        {
            lookup[(index, origin)] = foundTermiusTuple;
            lookup.Remove((index, terminus));
            lookupReversed[foundTermiusTuple] = (index, origin);
        }

        else if (lookupReversed.TryGetValue((index, origin), out foundOriginTuple))
        {
            lookupReversed[(index, terminus)] = foundOriginTuple;
            lookupReversed.Remove(foundOriginTuple);
            lookup[foundOriginTuple] = (index, terminus);
        }

        else
        {
            lookup.Add((index, origin), (index, terminus));
            lookupReversed.Add((index, terminus), (index, origin));
        }
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
