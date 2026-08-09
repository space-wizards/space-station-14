

namespace Content.Shared.Humanoid.SkinColoration;

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
    /// A value derived by dividing 1 by 255.
    /// Due to the way these values are stored and deconstructed we can't expect much more precision than this..
    /// </summary>
    public const float Epsilon = 0.003921568627451f;

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
