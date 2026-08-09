using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.SkinColoration;

/// <summary>
/// Coloration strategy that clamps the color within the HSL colorspace.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ClampedHslColoration : ISkinColorationStrategy
{
    /// <summary>
    /// Defines the valid (min, max) range for the hue channel (0.0 to 1.0).
    /// If min > max, the range wraps around 1.0 (e.g., for reds).
    /// </summary>
    [DataField]
    public (float, float)? Hue;

    /// <summary>
    /// The (min, max) of the saturation channel.
    /// </summary>
    [DataField]
    public (float, float)? Saturation;

    /// <summary>
    /// The (min, max) of the lightness channel.
    /// </summary>
    [DataField]
    public (float, float)? Lightness;

    public SkinColorationStrategyInput InputType => SkinColorationStrategyInput.Color;

    public bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        var hsl = Color.ToHsl(color);

        if (Hue is (var minHue, var maxHue) && !SkinColorationUtils.IsHueInRange(hsl.X, minHue, maxHue))
        {
            reason = $"Hue {Hue} is outside of range of min {minHue} max {maxHue}";
            return false;
        }

        if (Saturation is (var minSat, var maxSat) && (hsl.Y < minSat - SkinColorationUtils.Epsilon || hsl.Y > maxSat + SkinColorationUtils.Epsilon))
        {
            reason = $"Saturation {Saturation} is outside of range of min {minSat} max {maxSat}";
            return false;
        }

        if (Lightness is (var minLight, var maxLight) && (hsl.Z < minLight - SkinColorationUtils.Epsilon || hsl.Z > maxLight + SkinColorationUtils.Epsilon))
        {
            reason = $"Lightness {Lightness} is outside of range of min {minLight} max {maxLight}";
            return false;
        }

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        var hsl = Color.ToHsl(color);
        var oldHsl = hsl;

        if (Hue is (var minHue, var maxHue))
            hsl.X = SkinColorationUtils.ClampHue(hsl.X, minHue, maxHue);
        if (Saturation is (var minSat, var maxSat))
            hsl.Y = Math.Clamp(hsl.Y, minSat, maxSat);
        if (Lightness is (var minLight, var maxLight))
            hsl.Z = Math.Clamp(hsl.Z, minLight, maxLight);

        // If we're within bounds, don't add inaccuracy from an HSV round trip.
        if (hsl == oldHsl)
            return color;

        return Color.FromHsl(hsl);
    }
}
