using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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
    public Vector2 AngularRange { get; set; } = new Vector2(0f, 2 * MathF.PI);

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
    /// This container arranges its children, evenly separated, in a radial pattern
    /// </summary>
    public RadialContainer()
    {

    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // Determine the size of the arc, accounting for clockwise and anti-clockwise arrangements
        var arc = AngularRange.Y - AngularRange.X;
        arc = (arc < 0) ? MathF.Tau + arc : arc;
        arc = (RadialAlignment == RAlignment.AntiClockwise) ? MathF.Tau - arc : arc;

        // Account for both circular arrangements and arc-based arrangements
        var childMod = (MathHelper.CloseTo(arc, MathF.Tau, 0.01f)) ? 0 : 1;

        // Determine the separation between child elements
        var sepAngle = arc / (ChildCount - childMod);
        sepAngle *= (RadialAlignment == RAlignment.AntiClockwise) ? -1f : 1f;

        // Adjust the positions of all the child elements
        for (int i = 0; i < ChildCount; i++)
        {
            var child = GetChild(i);
            var position = new Vector2(Radius * MathF.Sin(AngularRange.X + sepAngle * i) + Width / 2f - child.Width / 2f, -Radius * MathF.Cos(AngularRange.X + sepAngle * i) + Height / 2f - child.Height / 2f);

            SetPosition(child, position);
        }
    }

    /// <summary>
    /// Specifies the different radial alignment modes
    /// </summary>
    /// <seealso cref="RadialAlignment"/>
    public enum RAlignment
    {
        Clockwise,
        AntiClockwise,
    }
}
