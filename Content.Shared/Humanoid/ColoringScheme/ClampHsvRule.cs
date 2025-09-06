namespace Content.Shared.Humanoid.ColoringScheme;

/// <summary>
/// Restricts the input color into the HSV range defined by <see cref="MinColor"/> and <see cref="MaxColor"/>.
/// Hue, saturation, and value are each clamped independently.
/// Use this rule when you want to allow only a specific slice of the HSV spectrum.
/// </summary>
public sealed partial class ClampHsvRule : ColoringSchemeRule
{
    /// <summary>
    /// Lower boundary of the allowed HSV range, expressed as a color.
    /// The hue, saturation, and value of this color define the minimums.
    /// Example: a dark green will enforce a minimum greenish tone.
    /// </summary>
    [DataField] public Color MinColor { get; set; } = Color.Black;

    /// <summary>
    /// Upper boundary of the allowed HSV range, expressed as a color.
    /// The hue, saturation, and value of this color define the maximums.
    /// Example: a bright yellow will cap how far the color may drift.
    /// </summary>
    [DataField] public Color MaxColor { get; set; } = Color.White;

    public override Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);
        var min = Color.ToHsv(MinColor);
        var max = Color.ToHsv(MaxColor);

        // Validate ranges
        if (min.X > max.X)
            Swap(ref min.X, ref max.X);
        if (min.Y > max.Y)
            Swap(ref min.Y, ref max.Y);
        if (min.Z > max.Z)
            Swap(ref min.Z, ref max.Z);

        hsv.X = Math.Clamp(hsv.X, min.X, max.X);
        hsv.Y = Math.Clamp(hsv.Y, min.Y, max.Y);
        hsv.Z = Math.Clamp(hsv.Z, min.Z, max.Z);

        return Color.FromHsv(hsv);
    }
}
