using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

/// <summary>
/// A prototype containing a SkinColorationStrategy
/// </summary>
[Prototype]
public sealed partial class SkinColorationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The skin coloration strategy specified by this prototype
    /// </summary>
    [DataField(required: true)]
    public ISkinColorationStrategy Strategy = default!;
}

/// <summary>
/// The type of input taken by a <see cref="ISkinColorationStrategy" />
/// </summary>
[Serializable, NetSerializable]
public enum SkinColorationStrategyInput
{
    /// <summary>
    /// A single floating point number from 0 to 100 (inclusive)
    /// </summary>
    Unary,

    /// <summary>
    /// A <see cref="Color" />
    /// </summary>
    Color,
}

/// <summary>
/// Takes in the given <see cref="SkinColorationStrategyInput" /> and returns an adjusted Color
/// </summary>
public interface ISkinColorationStrategy
{
    /// <summary>
    /// The type of input expected by the implementor; callers should consult InputType before calling the methods that require a given input
    /// </summary>
    SkinColorationStrategyInput InputType { get; }

    /// <summary>
    /// Returns whether or not the provided <see cref="Color" /> is within bounds of this strategy
    /// </summary>
    bool VerifySkinColor(Color color);

    /// <summary>
    /// Returns the closest skin color that this strategy would provide to the given <see cref="Color" />
    /// </summary>
    Color ClosestSkinColor(Color color);

    /// <summary>
    /// Returns the input if it passes <see cref="VerifySkinColor">, otherwise returns <see cref="ClosestSkinColor" />
    /// </summary>
    Color EnsureVerified(Color color)
    {
        if (VerifySkinColor(color))
        {
            return color;
        }

        return ClosestSkinColor(color);
    }

    /// <summary>
    /// Returns a colour representation of the given unary input
    /// </summary>
    Color FromUnary(float unary)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }

    /// <summary>
    /// Returns a colour representation of the given unary input
    /// </summary>
    float ToUnary(Color color)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }
}

/// <summary>
/// Unary coloration strategy that returns human skin tones, with 0 being lightest and 100 being darkest
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanTonedSkinColoration : ISkinColorationStrategy
{
    [DataField]
    public Color ValidHumanSkinTone = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    public SkinColorationStrategyInput InputType => SkinColorationStrategyInput.Unary;

    public bool VerifySkinColor(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);
        // rangeOffset makes it so that this value
        // is 25 <= hue <= 45
        if (hue < 25f || hue > 45f)
        {
            return false;
        }

        // rangeOffset makes it so that these two values
        // are 20 <= sat <= 100 and 20 <= val <= 100
        // where saturation increases to 100 and value decreases to 20
        if (sat < 20f || val < 20f)
        {
            return false;
        }

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        return ValidHumanSkinTone;
    }

    public Color FromUnary(float color)
    {
        // 0 - 100, 0 being gold/yellowish and 100 being dark
        // HSV based
        //
        // 0 - 20 changes the hue
        // 20 - 100 changes the value
        // 0 is 45 - 20 - 100
        // 20 is 25 - 20 - 100
        // 100 is 25 - 100 - 20

        var tone = Math.Clamp(color, 0f, 100f);

        var rangeOffset = tone - 20f;

        var hue = 25f;
        var sat = 20f;
        var val = 100f;

        if (rangeOffset <= 0)
        {
            // First 20 values adjust hue.
            hue += Math.Abs(rangeOffset);
        }
        else
        {
            // Remaining 80 values adjust saturation and value.
            sat += rangeOffset;
            val -= rangeOffset;
        }

        return Color.FromHsv(new Vector4(hue / 360f, sat / 100f, val / 100f, 1.0f));
    }

    public float ToUnary(Color color)
    {
        var hsv = Color.ToHsv(color);
        // check for hue/value first, if hue is lower than this percentage
        // and value is 1.0
        // then it'll be hue
        if (Math.Clamp(hsv.X, 25f / 360f, 1) > 25f / 360f
            && hsv.Z == 1.0)
        {
            return Math.Abs(45 - (hsv.X * 360));
        }
        // otherwise it'll directly be the saturation
        else
        {
            return hsv.Y * 100;
        }
    }
}

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

    public bool VerifySkinColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Hue is (var minHue, var maxHue) && !SkinColorationUtils.IsHueInRange(hsv.X, minHue, maxHue))
            return false;

        if (Saturation is (var minSat, var maxSat) && (hsv.Y < minSat - SkinColorationUtils.Epsilon || hsv.Y > maxSat + SkinColorationUtils.Epsilon))
            return false;

        if (Value is (var minVal, var maxVal) && (hsv.Z < minVal - SkinColorationUtils.Epsilon || hsv.Z > maxVal + SkinColorationUtils.Epsilon))
            return false;

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Hue is (var minHue, var maxHue))
            hsv.X = SkinColorationUtils.ClampHue(hsv.X, minHue, maxHue);
        if (Saturation is (var minSat, var maxSat))
            hsv.Y = Math.Clamp(hsv.Y, minSat, maxSat);
        if (Value is (var minVal, var maxVal))
            hsv.Z = Math.Clamp(hsv.Z, minVal, maxVal);

        return Color.FromHsv(hsv);
    }
}

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

    public bool VerifySkinColor(Color color)
    {
        var hsl = Color.ToHsl(color);

        if (Hue is (var minHue, var maxHue) && !SkinColorationUtils.IsHueInRange(hsl.X, minHue, maxHue))
            return false;

        if (Saturation is (var minSat, var maxSat) && (hsl.Y < minSat - SkinColorationUtils.Epsilon || hsl.Y > maxSat + SkinColorationUtils.Epsilon))
            return false;

        if (Lightness is (var minLight, var maxLight) && (hsl.Z < minLight - SkinColorationUtils.Epsilon || hsl.Z > maxLight + SkinColorationUtils.Epsilon))
            return false;

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        var hsl = Color.ToHsl(color);

        if (Hue is (var minHue, var maxHue))
            hsl.X = SkinColorationUtils.ClampHue(hsl.X, minHue, maxHue);
        if (Saturation is (var minSat, var maxSat))
            hsl.Y = Math.Clamp(hsl.Y, minSat, maxSat);
        if (Lightness is (var minLight, var maxLight))
            hsl.Z = Math.Clamp(hsl.Z, minLight, maxLight);

        return Color.FromHsl(hsl);
    }
}

/// <summary>
/// Contains shared utility methods for handling color manipulations in skin coloration strategies.
/// </summary>
internal static class SkinColorationUtils
{
    /// <summary>
    /// An empirically determined epsilon to account for floating-point drift during RGB -> HSL/HSV -> RGB conversions.
    /// Based on high-iteration testing (50M+ samples) which showed a max drift of ~4.9E-6 for HSL.
    /// A value of 1E-5f provides a robust safety margin.
    /// </summary>
    public const float Epsilon = 1e-5f; // 0.00001

    /// <summary>
    /// Checks if a hue value is within a specified range, correctly handling ranges that wrap around 1.0 (e.g., reds).
    /// </summary>
    /// <param name="hue">The hue value to check (0.0 to 1.0).</param>
    /// <param name="minHue">The minimum bound of the hue range.</param>
    /// <param name="maxHue">The maximum bound of the hue range.</param>
    /// <returns>True if the hue is within the range; otherwise, false.</returns>
    public static bool IsHueInRange(float hue, float minHue, float maxHue)
    {
        if (minHue > maxHue) // Wraps around 1.0 (e.g., reds)
            return hue >= minHue - Epsilon || hue <= maxHue + Epsilon;
        return hue >= minHue - Epsilon && hue <= maxHue + Epsilon;
    }

    /// <summary>
    /// Clamps a hue value to the closest boundary of a given range, correctly handling ranges that wrap around 1.0.
    /// </summary>
    /// <param name="hue">The hue value to clamp (0.0 to 1.0).</param>
    /// <param name="minHue">The minimum bound of the hue range.</param>
    /// <param name="maxHue">The maximum bound of the hue range.</param>
    /// <returns>The clamped hue value, adjusted to the nearest boundary if it was outside the valid range.</returns>
    public static float ClampHue(float hue, float minHue, float maxHue)
    {
        if (minHue > maxHue) // Wraps around 1.0
        {
            // If it's already in the valid range, do nothing.
            if (hue >= minHue || hue <= maxHue)
                return hue;

            // It's in the "invalid" gap between maxHue and minHue. Find the closest boundary.
            var mid = (maxHue + minHue) / 2f;
            if (hue > mid)
                return minHue;
            return maxHue;
        }

        return Math.Clamp(hue, minHue, maxHue);
    }
}
