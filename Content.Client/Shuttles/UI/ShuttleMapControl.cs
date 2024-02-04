using System.Numerics;
using Content.Client.Shuttles.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Handles the radar drawing part of the shuttle map.
/// </summary>
public sealed class ShuttleMapControl : BaseShuttleControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputs = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly SharedMapSystem _maps;
    private readonly ShuttleSystem _shuttles;
    private readonly SharedTransformSystem _xformSystem;

    protected override bool Draggable => true;

    public MapId ViewingMap = MapId.Nullspace;

    private EntityUid? _shuttleEntity;

    private readonly Font _font;

    private readonly EntityQuery<PhysicsComponent> _physicsQuery;
    private List<Entity<MapGridComponent>> _grids = new();

    private List<ShuttleBeacon> _destinations = new();
    private List<ShuttleExclusion> _exclusions = new();

    /// <summary>
    /// Toggles FTL mode on. This shows a pre-vis for FTLing a grid.
    /// </summary>
    public bool FtlMode;

    private Angle _ftlAngle;

    /// <summary>
    /// Raised when a request to FTL to a particular spot is raised.
    /// </summary>
    public event Action<MapCoordinates, Angle>? RequestFTL;

    public event Action<NetEntity, Angle>? RequestBeaconFTL;

    public ShuttleMapControl() : base(256f, 512f, 512f)
    {
        _maps = _entManager.System<SharedMapSystem>();
        _shuttles = _entManager.System<ShuttleSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();

        _physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public void SetMap(MapId mapId, Vector2 offset)
    {
        ViewingMap = mapId;
        Offset = offset;
        Recentering = false;
    }

    public void SetShuttle(EntityUid? entity)
    {
        _shuttleEntity = entity;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        // No move for you.
        if (FtlMode)
            return;

        base.MouseMove(args);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (FtlMode && ViewingMap != MapId.Nullspace)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                var mapUid = _mapManager.GetMapEntityId(ViewingMap);

                var beaconsOnly = _entManager.TryGetComponent(mapUid, out FTLDestinationComponent? destComp) &&
                                  destComp.BeaconsOnly;

                var mapTransform = Matrix3.CreateInverseTransform(Offset, Angle.Zero);

                if (beaconsOnly && TryGetBeacon(mapTransform, args.RelativePosition, PixelRect, out var foundBeacon, out _))
                {
                    RequestBeaconFTL?.Invoke(foundBeacon.Entity, _ftlAngle);
                }
                else
                {
                    // We'll send the "adjusted" position and server will adjust it back when relevant.
                    var mapCoords = new MapCoordinates(InverseMapPosition(args.RelativePosition), ViewingMap);
                    RequestFTL?.Invoke(mapCoords, _ftlAngle);
                }
            }
        }

        base.KeyBindUp(args);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        // Scroll handles FTL rotation if you're in FTL mode.
        if (FtlMode)
        {
            _ftlAngle += Angle.FromDegrees(15f) * args.Delta.Y;
            _ftlAngle = _ftlAngle.Reduced();
            return;
        }

        base.MouseWheel(args);
    }

    private void DrawParallax(DrawingHandleScreen handle)
    {
        if (!_entManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) || shuttleXform.MapUid == null)
            return;

        // TODO: Move this garbage to parallaxsystem or the likes.
        // Draw background texture
        var tex = _shuttles.GetTexture(shuttleXform.MapUid.Value);

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
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (ViewingMap == MapId.Nullspace)
            return;

        DrawRecenter();
        DrawParallax(handle);

        var viewedMapUid = _mapManager.GetMapEntityId(ViewingMap);
        var matty = Matrix3.CreateInverseTransform(Offset, Angle.Zero);
        var realTime = _timing.RealTime;
        var viewBox = new Box2(Offset - WorldRangeVector, Offset + WorldRangeVector);
        _grids.Clear();
        _mapManager.FindGridsIntersecting(ViewingMap, viewBox, ref _grids, approx: true, includeMap: false);

        // Draw our FTL range + no FTL zones
        // Do it up here because we want this layered below most things.
        if (FtlMode)
        {
            if (_entManager.TryGetComponent<TransformComponent>(_shuttleEntity, out var shuttleXform) &&
                shuttleXform.MapID == ViewingMap)
            {
                var gridUid = _shuttleEntity.Value;
                var gridPhysics = _physicsQuery.GetComponent(gridUid);
                var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(shuttleXform);
                gridPos = _maps.GetGridPosition((gridUid, gridPhysics), gridPos, gridRot);

                var gridRelativePos = matty.Transform(gridPos);
                gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
                var gridUiPos = ScalePosition(gridRelativePos);

                var range = _shuttles.GetFTLRange(gridUid);
                range *= MinimapScale;
                handle.DrawCircle(gridUiPos, range, Color.Gold, filled: false);
            }

            var exclusionColor = Color.Red;

            foreach (var exclusion in _exclusions)
            {
                // Check if it even intersects the viewport.
                var coords = _entManager.GetCoordinates(exclusion.Coordinates);
                var mapCoords = _xformSystem.ToMapCoordinates(coords);
                var enlargedBounds = viewBox.Enlarged(exclusion.Range);

                if (mapCoords.MapId != ViewingMap ||
                    !enlargedBounds.Contains(mapCoords.Position))
                {
                    continue;
                }

                var adjustedPos = matty.Transform(mapCoords.Position);
                var localPos = ScalePosition(adjustedPos with { Y = -adjustedPos.Y});
                handle.DrawCircle(localPos, exclusion.Range * MinimapScale, exclusionColor.WithAlpha(0.05f));
                handle.DrawCircle(localPos, exclusion.Range * MinimapScale, exclusionColor, filled: false);
            }
        }

        var verts = new Dictionary<Color, List<Vector2>>();
        var edges = new Dictionary<Color, List<Vector2>>();
        var strings = new Dictionary<Color, List<(Vector2, string)>>();

        // Constant size diamonds
        var diamondRadius = WorldRange / 40f;

        // If mouse highlighting a grid then show a highlight and support clicks

        // TODO:
        // Need per-map support for parallax textures (use component for path or default if none found), use nukies for planets
        // need FTL button
        // Rebuild should play a ping.
        // Need pre-vis for shuttle arrival spot
        // Need circular buffers for FTL

        foreach (var grid in _grids)
        {
            IFFComponent? iffComp = null;

            // Rudimentary IFF for now, if IFF hiding on then we don't show on the map at all
            if (grid.Owner != _shuttleEntity &&
                _entManager.TryGetComponent(grid, out iffComp) &&
                (iffComp.Flags & (IFFFlags.Hide | IFFFlags.HideLabel)) != 0x0)
            {
                continue;
            }

            var gridColor = _shuttles.GetIFFColor(grid, self: _shuttleEntity == grid.Owner, component: iffComp);

            var existingVerts = verts.GetOrNew(gridColor);
            var existingEdges = edges.GetOrNew(gridColor);

            var gridPhysics = _physicsQuery.GetComponent(grid.Owner);
            var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(grid.Owner);
            gridPos = _maps.GetGridPosition((grid, gridPhysics), gridPos, gridRot);

            var gridRelativePos = matty.Transform(gridPos);
            gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
            var gridUiPos = ScalePosition(gridRelativePos);

            var bottom = ScalePosition(gridRelativePos + new Vector2(0f, -2f * diamondRadius));
            var right = ScalePosition(gridRelativePos + new Vector2(diamondRadius, 0f));
            var top = ScalePosition(gridRelativePos + new Vector2(0f, 2f * diamondRadius));
            var left = ScalePosition(gridRelativePos + new Vector2(-diamondRadius, 0f));

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
            var iffText = _shuttles.GetIFFLabel(grid, component: iffComp);

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

        // Draw dotted line from our own shuttle entity to mouse.
        if (FtlMode)
        {
            var mousePos = _inputs.MouseScreenPosition;

            if (mousePos.Window != WindowId.Invalid)
            {
                var mouseLocalPos = GetLocalPosition(mousePos);
                var controlBounds = GlobalPixelRect;
                var controlLocalBounds = PixelRect;

                // If mouse inbounds then draw it.
                if (_shuttleEntity != null && controlBounds.Contains(mousePos.Position.Floored()) &&
                    _entManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) &&
                    shuttleXform.MapID != MapId.Nullspace)
                {
                    // If it's a beacon only map then snap the mouse to a nearby spot.
                    var beaconsOnly = _shuttles.IsBeaconMap(viewedMapUid);

                    ShuttleBeacon foundBeacon = default;

                    // Check for beacons around mouse and snap to that.
                    if (beaconsOnly && TryGetBeacon(matty, mouseLocalPos, controlLocalBounds, out foundBeacon, out var foundLocalPos))
                    {
                        mouseLocalPos = foundLocalPos;
                    }

                    var grid = _entManager.GetComponent<MapGridComponent>(_shuttleEntity.Value);

                    var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(shuttleXform);
                    gridPos = _maps.GetGridPosition(_shuttleEntity.Value, gridPos, gridRot);

                    // do NOT apply LocalCenter operation here because it will be adjusted in FTLFree.
                    var mouseMapPos = InverseMapPosition(mouseLocalPos);

                    var mapId = shuttleXform.MapID;
                    var ftlFree = (!beaconsOnly || foundBeacon != default) &&
                                  _shuttles.FTLFree(_shuttleEntity.Value, new EntityCoordinates(shuttleXform.MapUid!.Value, mouseMapPos), _ftlAngle, _exclusions);

                    var color = ftlFree ? Color.LimeGreen : Color.Magenta;

                    // Draw line from our shuttle to target (assuming same map).
                    if (mapId == ViewingMap)
                    {
                        var gridRelativePos = matty.Transform(gridPos);
                        gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
                        var gridUiPos = ScalePosition(gridRelativePos);

                        // Draw FTL buffer around the mouse.
                        var ourFTLBuffer = _shuttles.GetFTLBufferRange(_shuttleEntity.Value, grid);
                        ourFTLBuffer *= MinimapScale;
                        handle.DrawCircle(mouseLocalPos, ourFTLBuffer, Color.Magenta.WithAlpha(0.01f));
                        handle.DrawCircle(mouseLocalPos, ourFTLBuffer, Color.Magenta, filled: false);

                        // Might need to clip the line if it's too far? But my brain wasn't working so F.
                        handle.DrawDottedLine(gridUiPos, mouseLocalPos, color, (float) realTime.TotalSeconds * 30f);
                    }

                    // Draw shuttle pre-vis
                    var mouseVerts = new ValueList<Vector2>(4)
                    {
                        mouseLocalPos + _ftlAngle.RotateVec(new Vector2(0f, -2f * diamondRadius)) * MinimapScale,
                        mouseLocalPos + _ftlAngle.RotateVec(new Vector2(diamondRadius, 0f)) * MinimapScale,
                        mouseLocalPos + _ftlAngle.RotateVec(new Vector2(0f, 2f * diamondRadius)) * MinimapScale,
                        mouseLocalPos + _ftlAngle.RotateVec(new Vector2(-diamondRadius, 0f)) * MinimapScale,
                    };

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, mouseVerts.Span, color.WithAlpha(0.05f));
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, mouseVerts.Span, color);

                    // Draw a notch indicating direction.
                    var ftlLength = diamondRadius + 16f;
                    var ftlEnd = mouseLocalPos + _ftlAngle.RotateVec(new Vector2(0f, -ftlLength));

                    handle.DrawLine(mouseLocalPos, ftlEnd, color);
                }
            }
        }
    }

    private bool TryGetBeacon(Matrix3 mapTransform, Vector2 mousePos, UIBox2i area, out ShuttleBeacon foundBeacon, out Vector2 foundLocalPos)
    {
        // In pixels
        const float BeaconSnapRange = 32f;
        float nearestValue = float.MaxValue;
        foundLocalPos = Vector2.Zero;
        foundBeacon = default;

        foreach (var beacon in _destinations)
        {
            var beaconCoords = _xformSystem.ToMapCoordinates(_entManager.GetCoordinates(beacon.Coordinates));

            if (beaconCoords.MapId != ViewingMap)
                continue;

            // Invalid beacon?
            if (!_shuttles.CanFTLBeacon(beacon.Coordinates))
                continue;

            var position = mapTransform.Transform(beaconCoords.Position);
            var localPos = ScalePosition(position with {Y = -position.Y});

            // If beacon not on screen then ignore it.
            if (!area.Contains(localPos.Floored()))
                continue;

            var distance = (localPos - mousePos).Length();

            if (distance > BeaconSnapRange ||
                distance > nearestValue)
            {
                continue;
            }

            foundLocalPos = localPos;
            nearestValue = distance;
            foundBeacon = beacon;
        }

        return foundBeacon != default;
    }

    public void UpdateState(ShuttleConsoleBoundInterfaceState state)
    {
        _destinations = state.Destinations;
        _exclusions = state.Exclusions;
    }
}
