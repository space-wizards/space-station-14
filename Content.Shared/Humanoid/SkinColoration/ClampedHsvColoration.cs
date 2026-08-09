using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.SkinColoration;

/// <summary>
/// Coloration strategy that clamps the color within the HSV colorspace.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ClampedHsvColoration : ISkinColorationStrategy
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
    /// The (min, max) of the value channel.
    /// </summary>
    [DataField]
    public (float, float)? Value;

    public SkinColorationStrategyInput InputType => SkinColorationStrategyInput.Color;

    public bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        var hsv = Color.ToHsv(color);

        if (Hue is (var minHue, var maxHue) && !SkinColorationUtils.IsHueInRange(hsv.X, minHue, maxHue))
        {
            reason = $"Hue {Hue} is outside of range of min {minHue} max {maxHue}";
            return false;
        }

        if (Saturation is (var minSat, var maxSat) && (hsv.Y < minSat - SkinColorationUtils.Epsilon || hsv.Y > maxSat + SkinColorationUtils.Epsilon))
        {
            reason = $"Saturation {Saturation} is outside of range of min {minSat} max {maxSat}";
            return false;
        }

        if (Value is (var minVal, var maxVal) && (hsv.Z < minVal - SkinColorationUtils.Epsilon || hsv.Z > maxVal + SkinColorationUtils.Epsilon))
        {
            reason = $"Value {Value} is outside of range of min {minVal} max {maxVal}";
            return false;
        }

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        var hsv = Color.ToHsv(color);
        var oldHsv = hsv;

        if (Hue is (var minHue, var maxHue))
            hsv.X = SkinColorationUtils.ClampHue(hsv.X, minHue, maxHue);
        if (Saturation is (var minSat, var maxSat))
            hsv.Y = Math.Clamp(hsv.Y, minSat, maxSat);
        if (Value is (var minVal, var maxVal))
            hsv.Z = Math.Clamp(hsv.Z, minVal, maxVal);

        // If we're within bounds, don't add inaccuracy from an HSV round trip.
        if (hsv == oldHsv)
            return color;

        return Color.FromHsv(hsv);
    }
}
