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
            L = lightness / 100f
        };

        return (Color) o;
    }

    public static Color NudgeLightness(this Color c, float lightness)
    {
        var o = new OklabColor(c);
        o.L += lightness / 100f;

        return (Color) o;
    }
}
