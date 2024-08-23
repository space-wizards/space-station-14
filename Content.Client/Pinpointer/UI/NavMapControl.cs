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
using Robust.Shared.Utility;

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

    public NavMapData NavData = new();

    // Constants
    protected float UpdateTime = 1.0f;
    protected float MaxSelectableDistance = 10f;
    protected float MinDragDistance = 5f;
    protected static float MinDisplayedRange = 8f;
    protected static float MaxDisplayedRange = 128f;
    protected static float DefaultDisplayedRange = 48f;
    protected float MinmapScaleModifier = 0.075f;

    // Local variables
    private float _updateTimer = 1.0f;
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

        BackgroundColor = Color.FromSrgb(NavData.TileColor.WithAlpha(BackgroundOpacity));

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
        if (MapUid == null)
        {
            return;
        }

        NavData.UpdateNavMap(MapUid.Value);
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
            var worldPosition = Vector2.Transform(new Vector2(unscaledPosition.X, -unscaledPosition.Y) + offset, _transformSystem.GetWorldMatrix(_xform));

            // Find closest tracked entity in range
            var closestEntity = NetEntity.Invalid;
            var closestDistance = float.PositiveInfinity;

            foreach ((var currentEntity, var blip) in TrackedEntities)
            {
                if (!blip.Selectable)
                    continue;

                var currentDistance = (_transformSystem.ToMapCoordinates(blip.Coordinates).Position - worldPosition).Length();

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

        NavData.Offset = new Vector2(offset.X, -offset.Y);
        NavData.Draw(handle, ScalePosition, Box2.UnitCentered);

        // Invoke post wall drawing action
        PostWallDrawingAction?.Invoke(handle);

        // Beacons
        if (_beacons.Pressed)
        {
            var rectBuffer = new Vector2(5f, 3f);

            // Calculate font size for current zoom level
            var fontSize = (int) Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * _targetFontsize, 0);
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), fontSize);

            foreach (var beacon in _navMap.Beacons.Values)
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
                var mapPos = _transformSystem.ToMapCoordinates(coord);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = Vector2.Transform(mapPos.Position, _transformSystem.GetInvWorldMatrix(_xform)) - offset;
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

            var mapPos = _transformSystem.ToMapCoordinates(blip.Coordinates);

            if (mapPos.MapId != MapId.Nullspace)
            {
                var position = Vector2.Transform(mapPos.Position, _transformSystem.GetInvWorldMatrix(_xform)) - offset;
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
        // TODO: Sub to state changes.
        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            if (MapUid != null)
                NavData.UpdateNavMap(MapUid.Value);
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
