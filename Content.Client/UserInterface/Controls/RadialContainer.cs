using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialContainer : LayoutContainer
{
    /// <summary>
    /// Determines how far from the radial container's center all child elements will be placed
    /// </summary>
    public float Radius { get; set; } = 100f;

    public RadialContainer()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var sepAngle = 2f * MathF.PI / ChildCount;

        for (int i = 0; i < ChildCount; i++)
        {
            var child = GetChild(i);

            var position = new Vector2(Radius * MathF.Sin(sepAngle * i) + Width / 2 - child.Width / 2, -Radius * MathF.Cos(sepAngle * i) + Height / 2 - child.Height / 2);
            SetPosition(child, position);
        }
    }
}
