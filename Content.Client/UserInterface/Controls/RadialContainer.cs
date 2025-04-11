using Robust.Client.UserInterface.Controls;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialContainer : LayoutContainer
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClyde _clyde= default!;
    private readonly ShaderInstance _shader;

    private readonly float[] _angles = new float[64];
    private readonly float[] _sectorMedians = new float[64];
    private readonly Color[] _sectorColors = new Color[64];
    private readonly Color[] _borderColors = new Color[64];

    /// <summary>
    /// Increment of radius per child element to be rendered.
    /// </summary>
    private const float RadiusIncrement = 5f;

    /// <summary>
    /// Specifies the anglular range, in radians, in which child elements will be placed.
    /// The first value denotes the angle at which the first element is to be placed, and
    /// the second value denotes the angle at which the last element is to be placed.
    /// Both values must be between 0 and 2 PI radians
    /// </summary>
    /// <remarks>
    /// The top of the screen is at 0 radians, and the bottom of the screen is at PI radians
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 AngularRange
    {
        get => _angularRange;
        set
        {
            var x = value.X;
            var y = value.Y;

            x = x > MathF.Tau ? x % MathF.Tau : x;
            y = y > MathF.Tau ? y % MathF.Tau : y;

            x = x < 0 ? MathF.Tau + x : x;
            y = y < 0 ? MathF.Tau + y : y;

            _angularRange = new Vector2(x, y);
        }
    }

    private Vector2 _angularRange = new Vector2(0f, MathF.Tau - float.Epsilon);

    /// <summary>
    /// Determines the direction in which child elements will be arranged
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public RAlignment RadialAlignment { get; set; } = RAlignment.Clockwise;

    /// <summary>
    /// Radial menu radius determines how far from the radial container's center its child elements will be placed.
    /// To correctly display dynamic amount of elements control actually resizes depending on amount of child buttons,
    /// but uses this property as base value for final radius calculation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float InitialRadius { get; set; } = 100f;

    /// <summary>
    /// Radial menu radius determines how far from the radial container's center its child elements will be placed.
    /// This is dynamically calculated (based on child button count) radius, result of <see cref="InitialRadius"/> and
    /// <see cref="RadiusIncrement"/> multiplied by currently visible child button count.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CalculatedRadius { get; private set; }

    /// <summary>
    /// Determines radial menu button sectors inner radius, is a multiplier of <see cref="InitialRadius"/>.
    /// </summary>
    public float InnerRadiusMultiplier { get; set; } = 0.5f;

    /// <summary>
    /// Determines radial menu button sectors outer radius, is a multiplier of <see cref="InitialRadius"/>.
    /// </summary>
    public float OuterRadiusMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Sets whether the container should reserve a space on the layout for child which are not currently visible
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ReserveSpaceForHiddenChildren { get; set; } = true;

    /// <summary>
    /// This container arranges its children, evenly separated, in a radial pattern
    /// </summary>
    public RadialContainer()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("RadialMenu")
                                   .InstanceUnique();
    }

    /// <inheritdoc />
    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var children = ReserveSpaceForHiddenChildren
            ? Children
            : Children.Where(x => x.Visible);

        var childCount = children.Count();

        // Add padding from the center at higher child counts so they don't overlap.
        CalculatedRadius = InitialRadius + (childCount * RadiusIncrement);

        var isAntiClockwise = RadialAlignment == RAlignment.AntiClockwise;

        // Determine the size of the arc, accounting for clockwise and anti-clockwise arrangements
        var arc = AngularRange.Y - AngularRange.X;
        arc = arc < 0
            ? MathF.Tau + arc
            : arc;
        arc = isAntiClockwise
            ? MathF.Tau - arc
            : arc;

        // Account for both circular arrangements and arc-based arrangements
        var childMod = MathHelper.CloseTo(arc, MathF.Tau, 0.01f)
            ? 0
            : 1;

        // Determine the separation between child elements
        var sepAngle = arc / (childCount - childMod);
        sepAngle *= isAntiClockwise
            ? -1f
            : 1f;

        var controlCenter = finalSize * 0.5f;

        // Adjust the positions of all the child elements
        var query = children.Select((x, index) => (index, x));
        foreach (var (childIndex, child) in query)
        {
            const float angleOffset = MathF.PI * 0.5f;

            var targetAngleOfChild = AngularRange.X + sepAngle * (childIndex + 0.5f) + angleOffset;

            // flooring values for snapping float values to physical grid -
            // it prevents gaps and overlapping between different button segments
            var position = new Vector2(
                    MathF.Floor(CalculatedRadius * MathF.Cos(targetAngleOfChild)),
                    MathF.Floor(-CalculatedRadius * MathF.Sin(targetAngleOfChild))
                ) + controlCenter - child.DesiredSize * 0.5f + Position;

            SetPosition(child, position);

            // radial menu buttons with sector need to also know in which sector and around which point
            // they should be rendered, how much space sector should should take etc.
            if (child is IRadialMenuItemWithSector tb)
            {
                tb.AngleSectorFrom = sepAngle * childIndex;
                tb.AngleSectorTo = sepAngle * (childIndex + 1);
                tb.AngleOffset = angleOffset;
                tb.InnerRadius = CalculatedRadius * InnerRadiusMultiplier;
                tb.OuterRadius = CalculatedRadius * OuterRadiusMultiplier;
                tb.ParentCenter = controlCenter;
            }
        }

        return base.ArrangeOverride(finalSize);
    }

    /// <inheritdoc />
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        float selectedFrom = 0;
        float selectedTo = 0;

        var i = 0;
        foreach (var child in Children)
        {
            if (child is not IRadialMenuItemWithSector menuWithSector)
            {
                continue;
            }

            _angles[i] = menuWithSector.AngleSectorTo;
            _sectorMedians[i] = (menuWithSector.AngleSectorTo + menuWithSector.AngleSectorFrom) / 2;

            if (menuWithSector.IsHovered)
            {
                // menuWithSector.DrawBackground;
                // menuWithSector.DrawBorder;
                _sectorColors[i] = menuWithSector.HoverBackgroundColor;
                _borderColors[i] = menuWithSector.HoverBorderColor;
                selectedFrom = menuWithSector.AngleSectorFrom;
                selectedTo = menuWithSector.AngleSectorTo;
            }
            else
            {
                _sectorColors[i] = menuWithSector.BackgroundColor;
                _borderColors[i] = menuWithSector.BorderColor;
            }

            i++;
        }

        var screenSize = _clyde.ScreenSize;

        var menuCenter = new Vector2(
            ScreenCoordinates.X + (Size.X / 2) * UIScale,
            screenSize.Y - ScreenCoordinates.Y - (Size.Y / 2) * UIScale
        );

        _shader.SetParameter("separatorAngles", _angles);
        _shader.SetParameter("sectorMedianAngles", _sectorMedians);
        _shader.SetParameter("selectedFrom", selectedFrom);
        _shader.SetParameter("selectedTo", selectedTo);
        _shader.SetParameter("childCount", i);
        _shader.SetParameter("sectorColors", _sectorColors);
        _shader.SetParameter("borderColors", _borderColors);
        _shader.SetParameter("centerPos", menuCenter);
        _shader.SetParameter("screenSize", screenSize);
        _shader.SetParameter("innerRadius", CalculatedRadius * InnerRadiusMultiplier * UIScale);
        _shader.SetParameter("outerRadius", CalculatedRadius * OuterRadiusMultiplier * UIScale);

        handle.UseShader(_shader);
        handle.DrawRect(new UIBox2(0, 0, screenSize.X, screenSize.Y), Color.White);
        handle.UseShader(null);
    }

    /// <summary>
    /// Specifies the different radial alignment modes
    /// </summary>
    /// <seealso cref="RadialAlignment"/>
    public enum RAlignment : byte
    {
        Clockwise,
        AntiClockwise,
    }

}
