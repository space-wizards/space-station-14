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

        var oklab = c.LabFromSrgb();
        oklab.X = lightness;

        return oklab.LabToSrgb();
    }

    /// <summary>
    ///     Nudges the lightness (in OKLAB space) of <see cref="c" /> by <see cref="lightnessShift"/> / 100 units
    /// </summary>
    public static Color NudgeLightness(this Color c, float lightnessShift)
    {
        var oklab = c.LabFromSrgb();
        oklab.X = Math.Clamp(oklab.X + lightnessShift, 0, 1);

        return oklab.LabToSrgb();
    }

    /// <summary>
    ///     Nudges the chroma (in OKLCH space) of <see cref="c"/> by <see cref="chromaShift"/> / 100 units
    /// </summary>
    /// <remarks>
    ///     Referenced from https://github.com/jonathantneal/convert-colors/blob/master/src/lab-lch.js
    /// </remarks>
    public static Color NudgeChroma(this Color c, float chromaShift)
    {
        var oklab = c.LabFromSrgb();
        var oklch = Color.ToLch(oklab);

        oklch.Y = Math.Clamp(oklch.Y + chromaShift, 0, 1);

        return Color.FromLch(oklch).LabToSrgb();
    }

    /// <summary>
    ///     Blends two colors in the Oklab color space.
    /// </summary>
    public static Color OkBlend(this Color from, Color to, float factor)
    {
        DebugTools.Assert(factor is >= 0.0f and <= 1.0f);

        var okFrom = from.LabFromSrgb();
        var okTo = to.LabFromSrgb();

        var blended = Vector4.Lerp(okFrom, okTo, factor);
        return blended.LabToSrgb();
    }

    /// <summary>
    /// Converts a nonlinear sRGB ("normal") color to OkLAB.
    /// </summary>
    public static Vector4 LabFromSrgb(this Color from)
    {
        return Color.ToLab(Color.FromSrgb(from));
    }

    /// <summary>
    /// Converts OkLAB to a nonlinear sRGB ("normal") color.
    /// </summary>
    public static Color LabToSrgb(this Vector4 from)
    {
        return Color.ToSrgb(Color.FromLab(from).SimpleClipGamut());
    }

    /// <summary>
    /// Clips the gamut of the color so that all color channels are in the range 0 -> 1.
    /// </summary>
    /// <remarks>
    /// This uses no clever perceptual techniques, it literally just clamps the individual channels.
    /// </remarks>
    public static Color SimpleClipGamut(this Color from)
    {
        return new Color
        {
            R = Math.Clamp(from.R, 0, 1),
            G = Math.Clamp(from.G, 0, 1),
            B = Math.Clamp(from.B, 0, 1),
            A = from.A,
        };
    }
}
