namespace Content.Shared.Humanoid.ColoringScheme;

/// <summary>
/// Restricts the input color into the HSL range defined by <see cref="MinColor"/> and <see cref="MaxColor"/>.
/// Only saturation and lightness are clamped; hue remains untouched.
/// Use this rule when you want to control vibrancy (saturation) and brightness (lightness)
/// without locking the hue.
/// </summary>
[DataDefinition]
public sealed partial class ClampHslRule : ColoringSchemeRule
{
    /// <summary>
    /// Lower boundary of the allowed HSL range, expressed as a color.
    /// The saturation and lightness of this color define the minimums.
    /// </summary>
    [DataField] public Color MinColor { get; set; } = Color.Black;

    /// <summary>
    /// Upper boundary of the allowed HSL range, expressed as a color.
    /// The saturation and lightness of this color define the maximums.
    /// </summary>
    [DataField] public Color MaxColor { get; set; } = Color.White;

    public override Color Clamp(Color color)
    {
        var hsl = Color.ToHsl(color);
        var min = Color.ToHsl(MinColor);
        var max = Color.ToHsl(MaxColor);

        // Validate ranges
        if (min.Y > max.Y)
            Swap(ref min.Y, ref max.Y);
        if (min.Z > max.Z)
            Swap(ref min.Z, ref max.Z);

        hsl.Y = Math.Clamp(hsl.Y, min.Y, max.Y);
        hsl.Z = Math.Clamp(hsl.Z, min.Z, max.Z);

        return Color.FromHsl(hsl);
    }
}
