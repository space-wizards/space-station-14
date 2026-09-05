using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A horizontal or vertical rule. Its length is determined by the surrounding layout.
/// </summary>
public sealed class Separator : Control
{
    public const string StylePropertyColor = "separator-color";

    private static readonly Color DefaultColor = Color.FromHex("#3D4059");

    /// <summary>
    /// The direction along which the line extends.
    /// </summary>
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
    /// The line thickness in logical UI units. Must be finite and non-negative.
    /// Extra space across the line centers it without increasing its thickness.
    /// </summary>
    public float Thickness
    {
        get;
        set
        {
            if (!float.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (field == value)
                return;

            field = value;
            InvalidateMeasure();
        }
    } = 2;

    /// <summary>
    /// Optional color override. Set to null to use the current stylesheet again.
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
