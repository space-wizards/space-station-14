using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialContainer : LayoutContainer
{
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
        get
        {
            return _angularRange;
        }

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
    /// Determines how far from the radial container's center that its child elements will be placed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Radius { get; set; } = 100f;

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

    }

    protected override void Draw(DrawingHandleScreen handle)
    {
		const float baseRadius = 100f;
        const float radiusIncrement = 5f;
		
        var children = ReserveSpaceForHiddenChildren
            ? Children
            : Children.Where(x => x.Visible);

        // elements are children (buttons) and dividers, they all are spread evenly
        var elementCount = children.Count() * 2;
		
		// Add padding from the center at higher child counts so they don't overlap.
		Radius = baseRadius + (elementCount * radiusIncrement);

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
        var sepAngle = arc / (elementCount - childMod);
        sepAngle *= isAntiClockwise
            ? -1f
            : 1f;

        var halfWidth = Width / 2f;
        var halfHeight = Height / 2f;

        // Adjust the positions of all the child elements
        var query = children.Select((x, index) => (index, x));

        foreach (var (index, child) in query)
        {
            var indexForChildElement = index * 2 + 1;

            var targetAngleOfChild = AngularRange.X + sepAngle * indexForChildElement;
            var childX = Radius * MathF.Sin(targetAngleOfChild) + halfWidth - child.Width / 2f;
            var childY = -Radius * MathF.Cos(targetAngleOfChild) + halfHeight - child.Height / 2f;
            var position = new Vector2(childX, childY);

            SetPosition(child, position);

            if (child is RadialMenuTextureButton tb)
            {
                tb.AngleSectorFrom = sepAngle * (indexForChildElement - 1);
                tb.AngleSectorTo = sepAngle * (indexForChildElement + 1);
            }

            var positionOfSeparator = index * 2;

            var targetAngleOfSeparator = AngularRange.X + sepAngle * positionOfSeparator;
            var separatorSin = MathF.Sin(targetAngleOfSeparator);
            var separatorCos = MathF.Cos(targetAngleOfSeparator);
            handle.DrawLine(
                new Vector2(
                    Radius/2 * separatorSin + halfWidth,
                    -Radius/2 * separatorCos + halfHeight
                ) * UIScale,
                new Vector2(
                    Radius * 2 * separatorSin + halfWidth,
                    -Radius * 2 * separatorCos + halfHeight
                ) * UIScale,
                new Color(173, 216, 230, 180) // todo: use stylesheets
            );
        }

        base.Draw(handle);
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
