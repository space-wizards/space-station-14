using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Client._FTL.Weapons;

/// <summary>
/// Displays the weapon map data of the specified grid.
/// </summary>
public sealed class WeaponMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public EntityUid? MapUid;
    public List<EntityUid>? MapUids;
    private bool SelectLoaded = false;

    /// <summary>
    /// Raised if the user right-clicks on the radar control with the relevant entitycoordinates.
    /// </summary>
    public Action<EntityCoordinates>? OnWeaponMapClick;

    public Action<EntityCoordinates>? OnWeaponMapFire;

    public Action<EntityUid>? OnGridSwitchRequest;

    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();

    private Vector2 _offset;
    private bool _draggin;

    private bool _recentering = false;

    private float _recenterMinimum = 0.05f;
    private EntityCoordinates? _lastTargetCoordinates;

    private EntityCoordinates? _coordinates;
    private Angle? _rotation;

    // TODO: https://github.com/space-wizards/RobustToolbox/issues/3818
    private readonly Label _flavor = new()
    {
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Center,
        Margin = new Thickness(8f, 8f)
    };

    private readonly OptionButton _selectMapUid = new()
    {
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Left,
        Margin = new Thickness(8f, 4f)
    };

    public readonly Button FireButton = new()
    {
        Text = Loc.GetString("weapon-control-ui-button-fire-text"),
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    public WeaponMapControl() : base(8f, 128f, 48f)
    {
        IoCManager.InjectDependencies(this);
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
                FireButton,
                _selectMapUid,
                // _flavor,
            }
        };

        var topContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                topPanel,
                new Control
                {
                    Name = "DrawingControl",
                    VerticalExpand = true,
                    Margin = new Thickness(5f, 5f)
                }
            }
        };


        _flavor.Text = Loc.GetString($"weapon-control-ui-flavor-{_robustRandom.Next(1, 5)}");
        FireButton.OnPressed += args =>
        {
            if (_lastTargetCoordinates != null)
                OnWeaponMapFire?.Invoke(_lastTargetCoordinates.Value);
        };

        AddChild(topContainer);
        topPanel.Measure(Vector2.Infinity);
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.Use)
        {
            // fuck it we ball
            //_draggin = true;
        }

        if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            var coords = GetMouseCoordinates(_inputManager.MouseScreenPosition);
            OnWeaponMapClick?.Invoke(coords);
        }
    }


    /// <summary>
    /// Gets the entitycoordinates of where the mouseposition is, relative to the control, accounting for zoom.
    /// </summary>
    [PublicAPI]
    public EntityCoordinates GetMouseCoordinates(ScreenCoordinates screen)
    {
        if (_coordinates == null || _rotation == null)
        {
            return EntityCoordinates.Invalid;
        }

        if (_lastTargetCoordinates != null)
        {
            TrackedCoordinates.Remove(_lastTargetCoordinates.Value);
        }

        var pos = screen.Position / UIScale - GlobalPosition;
        var a = InverseScalePosition(pos);

        // TODO: Fix the offset going in the opposite position relative to the origin (which is the center)?????? Wtf????
        // TODO: Fix the large offset????? what the FUCK is this code
        var matrix = Matrix3.CreateTransform(a, _rotation.Value, new Vector2 { X = WorldRange, Y = WorldRange });
        var b = matrix.Transform(_offset);
        // making this positive does a weird ass offset relative to origin thing which is accurate to the cursor but not. this makes it static but with an offset so whatever
        var relativeWorldPos = -new Vector2(b.X, -b.Y / 2);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);

        var coords = _coordinates.Value.Offset(relativeWorldPos);

        TrackedCoordinates.Add(coords, (true, Color.Blue));
        Logger.Debug(coords.ToString());
        _lastTargetCoordinates = coords;

        return coords;
    }

    private Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidPoint) / MinimapScale;
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
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (MapUids != null && !SelectLoaded)
        {
            Logger.Debug("map uids work");
            for (var i = 0; i < MapUids.Count; i++)
            {
                var map = MapUids[i];
                _entityManager.TryGetComponent<MetaDataComponent>(map, out var meta);
                if (meta == null)
                    continue;
                _selectMapUid.AddItem(meta.EntityName, i);
                if (map == MapUid)
                    _selectMapUid.SelectId(i);
            }

            _selectMapUid.OnItemSelected += item =>
            {
                MapUid = MapUids[item.Id];
                OnGridSwitchRequest?.Invoke(MapUid.Value);
            };
            SelectLoaded = true;
        }

        Logger.Debug((MapUid == null).ToString());

        if (MapUid != null)
        {
            Logger.Debug(MapUid.Value.ToString());

            DrawGrid(handle, MapUid.Value);
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, EntityUid mapGridUid)
    {
        Logger.Debug(mapGridUid.ToString());
        if (_recentering)
        {
            var frameTime = Timing.FrameTime;
            var diff = _offset * (float) frameTime.TotalSeconds;

            if (_offset.LengthSquared < _recenterMinimum)
            {
                _offset = Vector2.Zero;
                _recentering = false;
            }
            else
            {
                _offset -= diff * 5f;
            }
        }

        if (!_entManager.TryGetComponent<NavMapComponent>(mapGridUid, out var navMap) ||
            !_entManager.TryGetComponent<TransformComponent>(mapGridUid, out var xform) ||
            !_entManager.TryGetComponent<MapGridComponent>(mapGridUid, out var grid))
        {
            return;
        }

        var offset = _offset;
        var tileColor = new Color(30, 67, 30);
        var lineColor = new Color(102, 217, 102);

        if (_entManager.TryGetComponent<PhysicsComponent>(mapGridUid, out var physics))
        {
            offset += physics.LocalCenter;
        }

        // Draw tiles
        if (_entManager.TryGetComponent<FixturesComponent>(mapGridUid, out var manager))
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

                    handle.DrawCircle(position, MinimapScale / 2f, value.Color);
                }
            }
        }
    }

    private Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidPoint;
    }
}
