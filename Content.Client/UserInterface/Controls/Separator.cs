using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A horizontal or vertical separator.
/// </summary>
public sealed class Separator : Control
{
    public const string StylePropertyColor = "separator-color";

    private static readonly Color DefaultColor = Color.FromHex("#3D4059");

    public OrientationMode Orientation
    {
        get;
        set
        {
            if (value is not (OrientationMode.Horizontal or OrientationMode.Vertical))
                throw new ArgumentOutOfRangeException(nameof(value));

            if (field == value)
                return;

            field = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Line thickness in UI units. The line is centered when the layout allocates extra space.
    /// </summary>
    public float Thickness
    {
        get;
        set
        {
            if (!float.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (field.Equals(value))
                return;

            field = value;
            InvalidateMeasure();
        }
    } = 2;

    /// <summary>
    /// Overrides the stylesheet color when set.
    /// </summary>
    public Color? ColorOverride { get; set; }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return Orientation == OrientationMode.Horizontal
            ? new Vector2(0, Thickness)
            : new Vector2(Thickness, 0);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var color = ColorOverride ?? StylePropertyDefault(StylePropertyColor, DefaultColor);
        var size = (Vector2) PixelSize;
        if (Orientation == OrientationMode.Horizontal)
            size.Y = Math.Min(size.Y, Thickness * UIScale);
        else
            size.X = Math.Min(size.X, Thickness * UIScale);

        handle.DrawRect(UIBox2.FromDimensions((PixelSize - size) / 2, size), color);
    }

    public enum OrientationMode : byte
    {
        Horizontal,
        Vertical
    }
}
