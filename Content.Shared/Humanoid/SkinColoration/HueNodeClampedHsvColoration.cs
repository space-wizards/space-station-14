using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.SkinColoration;

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
        var hue = SkinColorationUtils.ClampHue(hsv.X, Nodes.First().Hue, Nodes.Last().Hue);

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
        var oldHsv = hsv;
        hsv.X = SkinColorationUtils.ClampHue(hsv.X, Nodes.First().Hue, Nodes.Last().Hue);

        var range = GetNodeValuesForHue(hsv.X);
        if (range == null)
            return color;

        hsv.Y = Math.Clamp(hsv.Y, range.Saturation.Min, range.Saturation.Max);
        hsv.Z = Math.Clamp(hsv.Z, range.Value.Min, range.Value.Max);

        // If we're within bounds, don't add inaccuracy from an HSV round trip.
        if (hsv == oldHsv)
            return color;

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

#region Structures
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
#endregion Structures
