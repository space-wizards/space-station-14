using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Colorspace;

[PublicAPI]
public static class ColorExtensions
{
    /// <summary>
    ///     Adjusts the lightness of a color using the Oklab color space.
    /// </summary>
    public static Color WithLightness(this Color c, float lightness)
    {
        DebugTools.Assert(lightness is >= 0.0f and <= 1.0f);

        var oklab = Color.ToLab(c);
        oklab.X = lightness;

        return Color.FromLab(oklab);
    }

    /// <summary>
    ///     Nudges the lightness (in OKLAB space) of <see cref="c" /> by <see cref="lightnessShift"/> / 100 units
    /// </summary>
    public static Color NudgeLightness(this Color c, float lightnessShift)
    {
        var oklab = Color.ToLab(c);
        oklab.X = Math.Clamp(oklab.X + lightnessShift, 0, 1);

        return Color.FromLab(oklab);
    }

    /// <summary>
    ///     Nudges the chroma (in OKLCH space) of <see cref="c"/> by <see cref="chromaShift"/> / 100 units
    /// </summary>
    /// <remarks>
    ///     Referenced from https://github.com/jonathantneal/convert-colors/blob/master/src/lab-lch.js
    /// </remarks>
    public static Color NudgeChroma(this Color c, float chromaShift)
    {
        var oklab = Color.ToLab(c);
        var oklch = Color.ToLch(oklab);

        oklch.Y = Math.Clamp(oklch.Y + chromaShift, 0, 1);

        return Color.FromLab(Color.FromLch(oklch));
    }

    /// <summary>
    ///     Blends two colors in the Oklab color space.
    /// </summary>
    public static Color OkBlend(this Color from, Color to, float factor)
    {
        DebugTools.Assert(factor is >= 0.0f and <= 1.0f);

        var okFrom = Color.ToLab(from);
        var okTo = Color.ToLab(to);

        var blended = Vector4.Lerp(okFrom, okTo, factor);
        return Color.FromLab(blended);
    }
}
