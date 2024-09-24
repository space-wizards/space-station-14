using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.Colorspace;

[PublicAPI]
public static class ColorExtensions
{
    /// <summary>
    ///     Adjusts the lightness of a color using the Oklab color space.
    /// </summary>
    public static Color WithLightness(this Color c, float lightness)
    {
        DebugTools.Assert(lightness is >= 0.0f and <= 100.0f);

        var o = new OklabColor(c)
        {
            L = lightness / 100f,
        };

        return (Color) o;
    }

    /// <summary>
    ///     Nudges the lightness (in OKLAB space) of <see cref="c" /> by <see cref="lightnessShift"/> / 100 units
    /// </summary>
    public static Color NudgeLightness(this Color c, float lightnessShift)
    {
        var o = new OklabColor(c);
        o.L += lightnessShift;

        return (Color) o;
    }

    /// <summary>
    ///     Nudges the chroma (in OKLCH space) of <see cref="c"/> by <see cref="chromaShift"/> / 100 units
    /// </summary>
    /// <remarks>
    ///     Referenced from https://github.com/jonathantneal/convert-colors/blob/master/src/lab-lch.js
    /// </remarks>
    public static Color NudgeChroma(this Color c, float chromaShift)
    {
        var o = new OklabColor(c);

        var chroma = float.Sqrt(o.A * o.A + o.B * o.B);
        var hue = float.Atan2(o.B, o.A);
        chroma += chromaShift;

        o.A = chroma * float.Cos(hue);
        o.B = chroma * float.Sin(hue);

        return (Color) o;
    }
}
