using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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

    //// <summary>
    ///     If true, will randomly generate realistic hair and eye colors.
    ///     Will also crush randomly generated colors down to the skin's luminosity
    ///     so markings don't appear too bright on darker skin.
    /// </summary>
    [DataField]
    public bool RealisticColors;

    /// <summary>
    ///     If true, will also squash hair and eye colors to the coloration strategy.
    /// </summary>
    [DataField]
    public bool SquashAllColors;
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
    /// Outs a reason if the verification fails.
    /// </summary>
    bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Returns the closest skin color that this strategy would provide to the given <see cref="Color" />
    /// </summary>
    Color ClosestSkinColor(Color color);

    /// <summary>
    /// Returns the input if it passes <see cref="VerifySkinColor">, otherwise returns <see cref="ClosestSkinColor" />
    /// </summary>
    Color EnsureVerified(Color color)
    {
        if (VerifySkinColor(color, out _))
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

    public bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);
        // rangeOffset makes it so that this value
        // is 25 <= hue <= 45
        if (hue < 25f || hue > 45f)
        {
            reason = $"Hue {hue} is outside of expected ranges 25 and 45.";
            return false;
        }

        // rangeOffset makes it so that these two values
        // are 20 <= sat <= 100 and 20 <= val <= 100
        // where saturation increases to 100 and value decreases to 20
        if (sat < 20f || val < 20f)
        {
            reason = "Saturation or value are below expected number of 20.";
            return false;
        }

        return true;
    }

    public Color ClosestSkinColor(Color color)
    {
        return FromUnary(ToUnary(color));
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
/// Coloration strategy that clamps the color between nodes within the HSV colorspace.
/// Clamped values depend on the nodes and are linearly interpolated between them.
/// </summary>
/// <remarks>
/// For example:
/// A node at hue 0 with a saturation of [0,1] and a node at hue 1 with a staturation of [0.1,0.8]
/// At hue 0.5, the saturation would be clamped within [0.05,0.9]
/// </remarks>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HueNodeClampedHsvColoration : ISkinColorationStrategy
{
    // TODO: this is awful - why is it so large?
    /// <summary>
    /// The maximum amount of change to the saturation that we can expect between generating an HSV value
    /// at a threshold, converting it to RGB, then resaving it.
    /// Found experimentally by running HumanoidProfileTests.EnsureValidRandomSpecies("Vulpkanin") many times.
    /// </summary>
    /// <remarks>
    /// Due to RGB colors being clamped to 8 bits, precision is lost during transformation to HSL or HSV.
    /// The precision of the result _should be_ approximately 1/180.
    /// </remarks>
    public const float HSVTolerance = 0.019f;

    /// <summary>
    /// List of valid nodes in this coloration.
    /// </summary>
    [DataField(required: true)]
    public List<HueNodeClampedHsvColorationNode> Nodes = default!;

    public SkinColorationStrategyInput InputType => SkinColorationStrategyInput.Color;

    public bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        var hsv = Color.ToHsv(color);

        // Clamp the hue between the first and last node.
        // We don't want anything going outside of these values.
        var hue = SkinColorationUtils.ClampHue(hsv.X, Nodes.First().Hue,  Nodes.Last().Hue);

        var range = GetNodeValuesForHue(hue);

        // If no range was found, this color is invalid.
        if (range is null)
        {
            reason = "No valid range was found.";
            return false;
        }

        // If a range is found, check if the saturation is within the provided ranges.
        if (hsv.Y < range.Saturation.Min - HSVTolerance || hsv.Y > range.Saturation.Max + HSVTolerance)
        {
            reason = $"Saturation {hsv.Y} is outside of range of min {range.Saturation.Item1} max {range.Saturation.Item2}";
            return false;
        }

        // Check if the value is within provided ranges.
        if (hsv.Z < range.Value.Min - HSVTolerance || hsv.Z > range.Value.Max + HSVTolerance)
        {
            reason = $"Value {hsv.Z} is outside of range of min {range.Value.Min} max {range.Value.Max}";
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public Color ClosestSkinColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        // Clamp within specified nodes.
        hsv.X = SkinColorationUtils.ClampHue(hsv.X, Nodes.First().Hue,  Nodes.Last().Hue);

        var range = GetNodeValuesForHue(hsv.X);
        if (range == null)
            return color;

        hsv.Y = Math.Clamp(hsv.Y, range.Saturation.Min, range.Saturation.Max);
        hsv.Z = Math.Clamp(hsv.Z, range.Value.Min, range.Value.Max);

        return Color.FromHsv(hsv);
    }

    /// <summary>
    /// Finds the nodes affecting value and saturation at a specified hue value.
    /// </summary>
    /// <param name="hue">The hue value at which to find the nodes. Between 0 and 1.</param>
    /// <returns>The nodes taking effect at the specified hue.</returns>
    private (HueNodeClampedHsvColorationNode Prev, HueNodeClampedHsvColorationNode Next)? GetAffectingNodes(float hue)
    {
        if (Nodes.Count == 0)
            return null;

        // If only one node is provided we just consider it to control all values.
        if (Nodes.Count == 1)
            return (Nodes.First(), Nodes.Last());

        for (int i = 0; i < Nodes.Count; i++)
        {
            // We get the currently iterated element.
            var current = Nodes[i];

            // If there is no element after this one, we just loop back to the first element.
            // Basically a node list of [0, 0.5] will fall back to node at 0 if the hue is ever higher than 0.5
            var next = Nodes.ElementAtOrDefault(i + 1) ?? Nodes.First();

            // Is the hue within the range of the nodes we're considering?
            if (!SkinColorationUtils.IsHueInRange(hue, current.Hue, next.Hue))
                continue;

            return (current, next);
        }

        return null;
    }

    /// <summary>
    /// Gets the values at which to clamp the value and saturation based on the given hue.
    /// </summary>
    /// <param name="hue">The hue for which to get the clamping values. Between 0 and 1.</param>
    /// <returns>Node containing the value and saturation clamping.</returns>
    private HueNodeClampedHsvColorationNode? GetNodeValuesForHue(float hue)
    {
        if (Nodes.Count == 0)
            return null;

        // No node is actually affecting this coloring, so it's invalid.
        if (GetAffectingNodes(hue) is not { } affectingNodes)
            return null;

        var firstNode = affectingNodes.Prev;
        var secondNode = affectingNodes.Next;

        // If both values are equal we just return 0f.
        // This is to prevent dividing by 0.
        var weight = MathHelper.CloseTo(firstNode.Hue, secondNode.Hue) ? 0f : (hue - firstNode.Hue) / (secondNode.Hue - firstNode.Hue);

        // I know this is also used to define the nodes, however it contains all the data necessary
        // And I don't think creating a new DataDefinition is worth it just to get rid of the hue from this one.
        var finalNode = new HueNodeClampedHsvColorationNode();
        finalNode.Hue = hue;

        finalNode.Saturation.Min = MathHelper.Lerp(firstNode.Saturation.Min, secondNode.Saturation.Min, weight);
        finalNode.Saturation.Max = MathHelper.Lerp(firstNode.Saturation.Max, secondNode.Saturation.Max, weight);

        finalNode.Value.Min = MathHelper.Lerp(firstNode.Value.Min, secondNode.Value.Min, weight);
        finalNode.Value.Max = MathHelper.Lerp(firstNode.Value.Max, secondNode.Value.Max, weight);

        return finalNode;
    }
}

/// <summary>
/// A node to be used with <see cref="HueNodeClampedHsvColoration"/>.
/// Represents a single point on the hue spectrum with corresponding clamping limits for saturation and value.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HueNodeClampedHsvColorationNode
{
    /// <summary>
    /// The point on the hue spectrum where this node is placed.
    /// Between 0 and 1.
    /// </summary>
    [DataField]
    public float Hue;

    /// <summary>
    /// Defines the (min, max) saturation on the provided node.
    /// </summary>
    [DataField]
    public (float Min, float Max) Saturation;

    /// <summary>
    /// Defines the (min, max) value on the provided node.
    /// </summary>
    [DataField]
    public (float Min, float Max) Value;
}

/// <summary>
/// Contains shared utility methods for handling color manipulations in skin coloration strategies.
/// </summary>
internal static class SkinColorationUtils
{
    /// <summary>
    /// A value derived by dividing 1 by 361, rounding down.
    /// Due to the way these values are stored and deconstructed we can't expect much more precision than this..
    /// </summary>
    public const float EpsilonHue = 0.00277f;

    /// <summary>
    /// A value derived by dividing 1 by 256.
    /// Due to the way these values are stored and deconstructed we can't expect much more precision than this..
    /// </summary>
    public const float Epsilon = 0.00390625f;

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
            return hue >= minHue - EpsilonHue || hue <= maxHue + EpsilonHue;
        return hue >= minHue - EpsilonHue && hue <= maxHue + EpsilonHue;
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
