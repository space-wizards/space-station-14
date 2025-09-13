namespace Content.Shared.Humanoid.ColoringScheme;

public sealed partial class HsvHueLimit : ColoringSchemeRule
{
    /// <summary>
    /// List of allowed hue ranges, each range is a tuple (min, max) in [0,1].
    /// Supports both normal and wrap-around ranges.
    /// </summary>
    [DataField]
    public List<(float Min, float Max)> Ranges { get; set; } = new();

    public override Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);

        var h = hsv.X;
        h = (h % 1f + 1f) % 1f; // normalize hue to [0,1)

        if (Ranges.Count == 0)
        {
            // no ranges defined → leave color as is
            return color;
        }

        // check if h already falls into one of the ranges
        foreach (var (min, max) in Ranges)
        {
            if (IsInsideRange(h, min, max))
            {
                hsv.X = h;
                return Color.FromHsv(hsv);
            }
        }

        // if not inside → clamp to the closest border of any range
        var closest = h;
        var bestDist = float.MaxValue;

        foreach (var (min, max) in Ranges)
        {
            var border = ClosestBorder(h, min, max);
            var dist = HueDistance(h, border);

            if (dist < bestDist)
            {
                bestDist = dist;
                closest = border;
            }
        }

        hsv.X = closest;
        return Color.FromHsv(hsv);
    }

    /// <summary>
    /// Checks if a hue value is inside the range [min,max], supporting wrap-around.
    /// </summary>
    private static bool IsInsideRange(float h, float min, float max)
    {
        if (min <= max)
            return h >= min && h <= max;

        // wrap-around case
        return h >= min || h <= max;
    }

    /// <summary>
    /// Returns the closest border (min or max) of a range to h.
    /// </summary>
    private static float ClosestBorder(float h, float min, float max)
    {
        var distToMin = HueDistance(h, min);
        var distToMax = HueDistance(h, max);
        return distToMin < distToMax ? min : max;
    }

    /// <summary>
    /// Circular distance between two hues in [0,1).
    /// </summary>
    private static float HueDistance(float a, float b)
    {
        var d = Math.Abs(a - b);
        return Math.Min(d, 1f - d);
    }
}
